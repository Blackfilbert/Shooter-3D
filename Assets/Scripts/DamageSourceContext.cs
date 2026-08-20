public static class DamageSourceContext
{
    private static int _explosionDamageDepth;
    private static int _weakPointDamageDepth;

    public static bool IsExplosionDamage => _explosionDamageDepth > 0;
    public static bool IsWeakPointDamage => _weakPointDamageDepth > 0;

    public static void BeginExplosionDamage()
    {
        _explosionDamageDepth++;
    }

    public static void EndExplosionDamage()
    {
        if (_explosionDamageDepth > 0)
            _explosionDamageDepth--;
    }

    public static void BeginWeakPointDamage()
    {
        _weakPointDamageDepth++;
    }

    public static void EndWeakPointDamage()
    {
        if (_weakPointDamageDepth > 0)
            _weakPointDamageDepth--;
    }
}
