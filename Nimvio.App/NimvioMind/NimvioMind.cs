namespace Nimvio;

internal sealed class NimvioMind
{
    private readonly NimvioProfile _profile;
    private int _tick;

    public NimvioMind(NimvioProfile profile) => _profile = profile;
    public float Energy => _profile.Energy;
    public float Curiosity => _profile.Curiosity;
    public float Boredom => _profile.Boredom;
    public float Happiness => _profile.Happiness;

    public void Tick(NimvioBehavior behavior, ActivityLevel activity)
    {
        if (++_tick % 60 != 0)
        {
            return;
        }

        var activeFactor = activity == ActivityLevel.Energetic ? 1.25f : activity == ActivityLevel.Calm ? .75f : 1f;
        var moving = behavior is NimvioBehavior.Walking or NimvioBehavior.Hopping or NimvioBehavior.ChasingCursor or NimvioBehavior.Thrown;

        _profile.Energy = Clamp(_profile.Energy + (behavior == NimvioBehavior.Sleeping ? .9f
            : behavior == NimvioBehavior.Feeding ? .42f : behavior == NimvioBehavior.SharingMilk ? .3f
            : moving ? -.34f * activeFactor : .09f));
        _profile.Boredom = Clamp(_profile.Boredom + (moving || IsFun(behavior) ? -.7f : .28f * activeFactor));
        _profile.Curiosity = Clamp(_profile.Curiosity + (behavior is NimvioBehavior.Searching or NimvioBehavior.Inspecting ? -.55f : .2f));
        _profile.Happiness = Clamp(_profile.Happiness - .035f + (IsFun(behavior) ? .12f : 0));
    }

    public void Comforted()
    {
        _profile.Happiness = Clamp(_profile.Happiness + 8);
        _profile.Boredom = Clamp(_profile.Boredom - 8);
    }

    public void Explored()
    {
        _profile.Curiosity = Clamp(_profile.Curiosity - 18);
        _profile.Boredom = Clamp(_profile.Boredom - 12);
    }

    public void Startled() => _profile.Happiness = Clamp(_profile.Happiness - 3);
    
    public void Socialized()
    {
        _profile.Happiness = Clamp(_profile.Happiness + 3);
        _profile.Boredom = Clamp(_profile.Boredom - 5);
    }

    public void CaughtCursor()
    {
        _profile.Happiness = Clamp(_profile.Happiness + 10);
        _profile.Boredom = Clamp(_profile.Boredom - 12);
    }

    public void MissedUser() => _profile.Happiness = Clamp(_profile.Happiness + 12);

    public void FeltIgnored() => _profile.Happiness = Clamp(_profile.Happiness - 4);

    private static bool IsFun(NimvioBehavior behavior) => behavior is NimvioBehavior.Pointing or NimvioBehavior.Waving
        or NimvioBehavior.ChasingCursor or NimvioBehavior.Inspecting or NimvioBehavior.PlayingTogether
        or NimvioBehavior.Hugging or NimvioBehavior.Competing or NimvioBehavior.Feeding or NimvioBehavior.SharingMilk;

    private static float Clamp(float value) => Math.Clamp(value, 0, 100);
}
