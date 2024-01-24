### The Long Shock mod for The Long Drive
This only works with PiShock, other shock devices will need to be adapted manually.

This mod expects you to have a config file right next to the DLL inside the mod folder called `shock.json`. Currently it does not support the settings GUI provided by TLD mod loader as there is a bug with loading it.

Example shock.json:
```json
{
    "username": "PiShock Username",
    "apiKey": "PiShock API Key",
    "shockerCode": "PiShock Shocker code",
    "baseShock": 5,
    "highValueParts": "felni,engine,trunk,coolanttank",
    "highValuePartBonusShock": 5,
    "bloodBonusShock": 5,
    "deathShockOverride": 20,
    "maxShockLimiter": 30,
    "shockLockoutTimeSeconds":15,
    "testMode": true
}
```