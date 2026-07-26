public static class CountFeedbackPolicy
{
    public static bool ShouldAnimate(bool hasDisplayedValue, int displayedValue, int incomingValue)
    {
        return hasDisplayedValue && incomingValue < displayedValue;
    }
}
