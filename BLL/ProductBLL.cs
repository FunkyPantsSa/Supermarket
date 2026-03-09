using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
    /// <summary>
    /// 商品业务逻辑层
    /// </summary>
    public class ProductBLL
    {
        private readonly ProductDAL _productDAL;

        public ProductBLL(string dbPath)
        {
            _productDAL = new ProductDAL(dbPath);
        }

        public List<Product> GetAllProducts()
        {
            return _productDAL.GetAllProducts();
        }

        public Product? GetProductById(int id)
        {
            return _productDAL.GetProductById(id);
        }

        public bool AddProduct(Product product, out string error)
        {
            error = "";
            if (product.ID <= 0)
            {
                error = "商品ID必须大于0。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                error = "商品名称不能为空。";
                return false;
            }

            var existing = _productDAL.GetProductById(product.ID);
            if (existing != null && !existing.IsDeleted)
            {
                error = "商品ID已存在。";
                return false;
            }

            _productDAL.SaveProduct(product);
            return true;
        }

        public bool UpdateProduct(Product product, out string error)
        {
            error = "";
            var existing = _productDAL.GetProductById(product.ID);
            if (existing == null)
            {
                error = "商品不存在。";
                return false;
            }

            _productDAL.SaveProduct(product);
            return true;
        }

        public bool DeleteProduct(int id, out string error)
        {
            error = "";
            var product = _productDAL.GetProductById(id);
            if (product == null)
            {
                error = "商品不存在。";
                return false;
            }

            _productDAL.LogicalDeleteProduct(id);
            return true;
        }

        public bool UpdateStock(int productId, int quantityChange, out string error)
        {
            error = "";
            var product = _productDAL.GetProductById(productId);
            if (product == null)
            {
                error = "商品不存在。";
                return false;
            }

            var newStock = product.StockCount + quantityChange;
            if (newStock < 0)
            {
                error = "库存不足。";
                return false;
            }

            _productDAL.UpdateProductStock(productId, quantityChange);
            return true;
        }

        public List<Product> GetLowStockProducts()
        {
            return _productDAL.GetAllProducts()
                .Where(p => p.StockCount <= p.LowStockThreshold)
                .ToList();
        }
    }
}
