public static class Env
{
    public enum Environment
    {
        DEV,
        STAGE,
        PROD
    }

    public static readonly Environment environment;

    public static readonly bool IsDebug = true;
}
