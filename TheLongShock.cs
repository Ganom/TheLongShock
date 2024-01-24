using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
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

        private readonly Queue<List<string>> _beforeCarParts = new Queue<List<string>>();

        private fpscontroller _player;
        private ShockHandler _shockHandler;
        private ConfigData _config;
        private float _lastSpeed;
        private float _crashDelta;
        private DateTime _lastShockSentTime;

        private List<string> _currentCarParts = new List<string>();
        [CanBeNull] private CrashEvent _crashEvent;
        private int _frameCounter;

        public override void OnGUI()
        {
            if (_error == "") return;

            GUI.Label(
                new Rect(75, 0, 2000, 3000),
                "<color=red><size=48>" +
                $"\nERROR PING GANOM:{_error}" +
                "</size></color>"
            );
        }

        public override void OnLoad()
        {
            _lastShockSentTime = DateTime.MinValue;
            _player = mainscript.M.player;
            _shockHandler = new GameObject("ShockHandler").AddComponent<ShockHandler>();
            _currentCarParts.Clear();
            _beforeCarParts.Clear();
            _beforeCarParts.Enqueue(new List<string>());
            _lastSpeed = 0;
            _crashDelta = 0;
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

                if (_crashEvent != null)
                {
                    _frameCounter++;

                    if (_frameCounter < 60) return;

                    CalculateLostPartsAndSendShock(_crashEvent);
                    _crashEvent = null;
                    _frameCounter = 0;
                    return;
                }

                DetectCrashUpdateLoop(car);
                CheckIfPartsWereLost();

                if (_beforeCarParts.Count >= 60)
                {
                    _beforeCarParts.Dequeue();
                }

                _beforeCarParts.Enqueue(_currentCarParts);
            }
            catch (Exception e)
            {
                _error = $"{e.Message}";
            }
        }

        private void CheckIfPartsWereLost()
        {
            var earliest = _beforeCarParts.Peek();
            var missingParts = GetListDifferences(earliest, _currentCarParts);

            if (!missingParts.Any())
            {
                return;
            }

            if (!(_crashDelta > 0)) return;

            _crashEvent = new CrashEvent
            {
                BeforeCrashParts = earliest,
                CrashDelta = _crashDelta
            };
            _crashDelta = 0;
        }

        private void CalculateLostPartsAndSendShock(CrashEvent @event)
        {
            if (_player.died)
            {
                WriteToLogFile(
                    $"death shock:{_config.deathShockOverride}," +
                    $"delta:{@event.CrashDelta}," +
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

            if (bonusShock == 0)
            {
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

            if (DateTime.Now - _lastShockSentTime < TimeSpan.FromSeconds(_config.shockLockoutTimeSeconds))
            {
                return;
            }

            WriteToLogFile(
                $"shock:{shockPercent}," +
                $"delta:{@event.CrashDelta}," +
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

        private void WriteToLogFile(string message)
        {
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\TheLongDrive\Mods\";
            var logFilePath = Path.Combine(docPath, "shock.log");

            using (var writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(message);
            }
        }
    }
}