using LaVaquita.Utils;

namespace LaVaquita.Controls;

public sealed class MontoEntry : Entry
{
    public MontoEntry()
    {
        TextChanged += (_, args) =>
        {
            if (!MoneyFormat.EsEntradaValida(args.NewTextValue))
                Text = args.OldTextValue;
        };
    }
}
