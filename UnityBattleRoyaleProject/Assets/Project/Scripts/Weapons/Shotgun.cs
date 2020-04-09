using System.Collections;
using System.Collections.Generic;

public class Shotgun : Weapon {
    private int amountOfBullets = 5;
    public int AmountOfBullets { get { return amountOfBullets; } }

    public Shotgun()
    {
        clipSize = 4;
        maxAmmunition = 12;
        reloadDuration = 3.0f;
        cooldownDuration = 1f;
        isAutomatic = false;
        name = "Shotgun";
        aimVariation = 0.08f;
        damage = 4;
    }
}
