namespace Shturman.Presentation;

public record UICompass(double degrees)
{
    public UICompass ChangeDegrees(double targetDegrees) => this with
    {
        degrees = targetDegrees
    };
}
