namespace LoaderFixtures;

public static class ManagedTestPackage
{
    public static string Echo(string value) => value;

    public static void Noop()
    {
    }

    public static int Overloaded(int value) => value;

    public static string Overloaded(string value) => value;

    public static void WithOut(out int value) => value = 0;

    public static int ByReference(ref int value) => value;

    public static decimal Decimal(decimal value) => value;
}

public static class OtherStatic
{
}
