namespace HotpotSort.Platform
{
    // Configuration readiness is separate from login, network and SDK availability.
    public enum WeChatCapabilityState { Disabled, NotConfigured, Ready, Unavailable }
}
