using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TLDLoader;
using UnityEngine;

namespace TheLongShockProper
{
    public class TheLongShock : Mod
    {
        public override string ID => "xdx";
        public override string Name => "The Long Shock";
        public override string Author => "ganom xdx";
        public override string Version => "1";

        private static string _error = "";
        private static string _crash = "";
        private static string _lostParts = "";
        private static float _test;

        private static readonly List<string> ShocksSent = new List<string>();

        private fpscontroller _player;
        private ShockHandler _shockHandler;
        private ConfigData _config;
        private float _lastSpeed;
        private float _crashDelta;
        private DateTime _lastShockSentTime;

        private List<string> _currentCarParts = new List<string>();
        private List<string> _beforeCarParts = new List<string>();

        public override void OnGUI()
        {
            if (_error == "")
            {
                GUI.Label(
                    new Rect(75, 0, 2000, 3000),
                    "<color=red><size=32>" +
                    $"\nTest:{_test}" +
                    $"\nCrash:{_crash}" +
                    $"\nLost Parts:{_lostParts}" +
                    $"\n{string.Join("\n", ShocksSent)}" +
                    "</size></color>"
                );
            }
            else
            {
                GUI.Label(
                    new Rect(75, 0, 2000, 3000),
                    "<color=red><size=48>" +
                    $"\nERROR PING GANOM:{_error}" +
                    "</size></color>"
                );
            }
        }

        public override void OnLoad()
        {
            _lastShockSentTime = DateTime.MinValue;
            _player = mainscript.M.player;
            _shockHandler = new GameObject("ShockHandler").AddComponent<ShockHandler>();
            ShocksSent.Clear();
            _currentCarParts.Clear();
            _beforeCarParts.Clear();
            _lastSpeed = 0;
            _crashDelta = 0;
            _crash = "";
            _lostParts = "";
            _test = 0;
            _error = "";

            InitConfig();
        }

        private void InitConfig()
        {
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\TheLongDrive\Mods\";
            var jsonFilePath = Path.Combine(docPath, "shock.json");

            if (!File.Exists(jsonFilePath)) return;

            var json = File.ReadAllText(jsonFilePath);
            _config = JsonUtility.FromJson<ConfigData>(json);
        }

        public override void Update()
        {
            try
            {
                var car = _player.Car;

                if (car == null)
                {
                    return;
                }

                _currentCarParts = CountCarParts(car);

                DetectCrashUpdateLoop(car);
                CheckIfPartsWereLost();

                _beforeCarParts = _currentCarParts;
            }
            catch (Exception e)
            {
                _error = $"{e.Message}";
            }
        }

        private void CheckIfPartsWereLost()
        {
            var missingParts = GetListDifferences(_beforeCarParts, _currentCarParts);

            if (!missingParts.Any())
            {
                return;
            }

            if (!(_crashDelta > 0)) return;

            var crashEvent = new CrashEvent
            {
                BeforeCrashParts = _beforeCarParts,
                CrashDelta = _crashDelta
            };

            CalculateLostPartsAndSendShock(crashEvent);
        }

        private void CalculateLostPartsAndSendShock(CrashEvent @event)
        {
            if (_player.died)
            {
                _lostParts = "";
                _test = _crashDelta;
                _crash = "HAH YOU DIED LOSER";
                _crashDelta = 0;
                ShocksSent.Add(
                    $"death shock:{_config.deathShockOverride}," +
                    $"delta:{_crashDelta}," +
                    $"timestamp:{DateTime.Now:dd_HH:mm:ss.fff}"
                );
                _shockHandler.SendShock(_config.deathShockOverride, _config);
                _lastShockSentTime = DateTime.Now;
                return;
            }

            var missingParts = GetListDifferences(@event.BeforeCrashParts, _currentCarParts);
            var highValuePartsArray = _config.highValueParts.Split(',');
            var bonusShock = missingParts
                .Where(missingPart => highValuePartsArray.Contains(missingPart))
                .Sum(missingPart => _config.highValuePartBonusShock);

            var missingPartsCsv = string.Join(",", missingParts);
            _lostParts = missingPartsCsv;
            _test = _crashDelta;

            if (bonusShock == 0)
            {
                _crashDelta = 0;
                _crash = $"False; no bonus. delta:{_crashDelta}.";
                return;
            }

            if (_player.Car.blood.ON)
            {
                bonusShock += _config.bloodBonusShock;
            }

            var shockPercent = Math.Min(
                Math.Max(
                    _config.baseShock,
                    _config.baseShock + bonusShock
                ),
                _config.maxShockLimiter
            );

            _lostParts = missingPartsCsv;
            _test = _crashDelta;
            _crash = $"True; shock:{shockPercent},delta:{_crashDelta},blood:{_player.Car.blood.ON},bonus:{bonusShock}";
            _crashDelta = 0;
            
            if (DateTime.Now - _lastShockSentTime < TimeSpan.FromSeconds(30)) 
            {
                return;
            }
            
            ShocksSent.Add(
                $"shock:{shockPercent}," +
                $"delta:{_crashDelta}," +
                $"blood:{_player.Car.blood.ON}," +
                $"bonus:{bonusShock}," +
                $"parts:{missingPartsCsv}," +
                $"timestamp:{DateTime.Now:dd_HH:mm:ss.fff}"
            );
            _lastShockSentTime = DateTime.Now;
            _shockHandler.SendShock(shockPercent, _config);
        }

        private void DetectCrashUpdateLoop(carscript car)
        {
            var crashMultiplier = car.crashMultiplier;
            var crashFallOffMultiplier = car.crashFallOffMultiplier;
            var minFalloffSpeed =
                mainscript.M.crashSpeedMaxFallOff *
                Mathf.Lerp(
                    1f,
                    0.2f,
                    Mathf.InverseLerp(
                        0.0f,
                        4f,
                        car.condition.state
                    )
                );
            var maxFalloffSpeed =
                mainscript.M.crashSpeedMinFallOff *
                Mathf.Lerp(
                    1f,
                    0.2f,
                    Mathf.InverseLerp(
                        0.0f,
                        4f,
                        car.condition.state
                    )
                );

            var v = Mathf.Abs(car.speed - _lastSpeed);

            _lastSpeed = car.speed;
            v *= crashMultiplier;

            if (!(v * crashFallOffMultiplier > UnityEngine.Random.Range(minFalloffSpeed, maxFalloffSpeed))) return;

            _crashDelta =
                Mathf.InverseLerp(
                    mainscript.M.crashSpeedMinFallOff,
                    mainscript.M.crashSpeedMaxFallOffMax,
                    v
                );
            _test = _crashDelta;
        }

        private static List<string> CountCarParts(carscript car)
        {
            var parts = new List<string>();

            foreach (var carPartSlot in car.gameObject.GetComponent<tosaveitemscript>().partslotscripts)
            {
                if (carPartSlot == null || carPartSlot.part == null) continue;

                var activePart = carPartSlot.part;

                parts.AddRange(
                    from subPartSlot in activePart.tosaveitem.partslotscripts
                    select subPartSlot.part
                    into subPart
                    where subPart != null
                    select subPart.tipus.First().Trim()
                );

                parts.Add(activePart.tipus.First().Trim());
            }

            return parts;
        }

        private static List<string> GetListDifferences(List<string> firstList, List<string> secondList)
        {
            var counts = new Dictionary<string, int>();
            var differences = new List<string>();

            foreach (var item in firstList)
            {
                if (counts.ContainsKey(item))
                {
                    counts[item]++;
                }
                else
                {
                    counts[item] = 1;
                }
            }

            foreach (var item in secondList.Where(item => counts.ContainsKey(item)))
            {
                counts[item]--;
                if (counts[item] == 0)
                {
                    counts.Remove(item);
                }
            }

            foreach (var pair in counts)
            {
                for (var i = 0; i < pair.Value; i++)
                {
                    differences.Add(pair.Key);
                }
            }

            return differences;
        }
    }
}