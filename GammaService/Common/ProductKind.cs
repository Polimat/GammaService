using System.ComponentModel;

namespace GammaService.Common
{
    public enum ProductKind
    {
        [Description("Тамбура")]
        ProductSpool,
        [Description("Паллеты")]
        ProductPallet,
        [Description("Групповые упаковки")]
        ProductGroupPack
    }
}
