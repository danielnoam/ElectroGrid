using NUnit.Framework;

public class ComboTests
{
    private Match3ComboSettings _settings;
    private Match3Combo _combo;

    [SetUp]
    public void SetUp()
    {
        _settings = new Match3ComboSettings
        {
            swapFill = 0.25f,
            cascadeFill = 0.1f,
            startWindow = 4f,
            windowShrink = 0.5f,
            minWindow = 1.5f
        };
        _combo = new Match3Combo(_settings);
    }

    // One full swap the way the game manager drives it
    private void Swap(int cascadeWaves = 0)
    {
        _combo.BeginResolve();
        _combo.AddSwapStep();
        for (int i = 0; i < cascadeWaves; i++) _combo.AddCascadeWave();
        _combo.EndResolve();
    }

    [Test]
    public void SwapAddsAStepAndFill()
    {
        Swap();

        Assert.AreEqual(1, _combo.Count);
        Assert.AreEqual(0.25f, _combo.Fill, 1e-5f);
        Assert.AreEqual(4f, _combo.TimeLeft, 1e-5f);
    }

    [Test]
    public void CascadesAddFillButNoStep()
    {
        Swap(cascadeWaves: 2);

        Assert.AreEqual(1, _combo.Count);
        Assert.AreEqual(0.45f, _combo.Fill, 1e-5f);
    }

    [Test]
    public void CascadeWithoutASwapIsIgnored()
    {
        _combo.AddCascadeWave();

        Assert.AreEqual(0, _combo.Count);
        Assert.AreEqual(0f, _combo.Fill);
    }

    [Test]
    public void WindowShrinksPerStepButNotBelowTheMinimum()
    {
        Swap();
        Assert.AreEqual(4f, _combo.Window, 1e-5f);
        Swap();
        Assert.AreEqual(2f, _combo.Window, 1e-5f);
        Swap();
        Assert.AreEqual(1.5f, _combo.Window, 1e-5f);
    }

    [Test]
    public void RunningOutOfTimeResetsTheCombo()
    {
        int resetAt = -1;
        _combo.Reset += count => resetAt = count;
        Swap();
        Swap();

        _combo.Tick(1.9f);
        Assert.AreEqual(2, _combo.Count, "still inside the 2s window");

        _combo.Tick(0.2f);
        Assert.AreEqual(0, _combo.Count);
        Assert.AreEqual(0f, _combo.Fill);
        Assert.AreEqual(2, resetAt);
    }

    [Test]
    public void CountdownIsPausedWhileTheBoardResolves()
    {
        Swap();
        _combo.BeginResolve();

        _combo.Tick(100f);
        Assert.AreEqual(1, _combo.Count);

        _combo.EndResolve();
        Assert.AreEqual(_combo.Window, _combo.TimeLeft, 1e-5f, "a settled board gives a full window again");
    }

    [Test]
    public void FullBarRaisesFilledAndStartsOverWithoutEndingTheCombo()
    {
        int filled = 0;
        _combo.Filled += () => filled++;

        for (int i = 0; i < 4; i++) Swap();

        Assert.AreEqual(1, filled);
        Assert.AreEqual(0f, _combo.Fill, 1e-5f);
        Assert.AreEqual(4, _combo.Count);
    }

    [Test]
    public void StepAddedReportsTheNewCount()
    {
        int last = 0;
        _combo.StepAdded += count => last = count;
        Swap();
        Swap();

        Assert.AreEqual(2, last);
    }

    [Test]
    public void ClearOnAnIdleComboRaisesNothing()
    {
        bool reset = false;
        _combo.Reset += _ => reset = true;

        _combo.Clear();

        Assert.IsFalse(reset);
    }
}
