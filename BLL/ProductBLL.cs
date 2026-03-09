using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
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
                error = "\u5546\u54C1ID\u5FC5\u987B\u5927\u4E8E0\u3002";
                return false;
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                error = "\u5546\u54C1\u540D\u79F0\u4E0D\u80FD\u4E3A\u7A7A\u3002";
                return false;
            }

            var existing = _productDAL.GetProductById(product.ID);
            if (existing != null && !existing.IsDeleted)
            {
                error = "\u5546\u54C1ID\u5DF2\u5B58\u5728\u3002";
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
                error = "\u5546\u54C1\u4E0D\u5B58\u5728\u3002";
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
                error = "\u5546\u54C1\u4E0D\u5B58\u5728\u3002";
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
                error = "\u5546\u54C1\u4E0D\u5B58\u5728\u3002";
                return false;
            }

            var newStock = product.StockCount + quantityChange;
            if (newStock < 0)
            {
                error = "\u5E93\u5B58\u4E0D\u8DB3\u3002";
                return false;
            }

            _productDAL.UpdateProductStock(productId, quantityChange);
            return true;
        }

        public List<Product> GetLowStockProducts()
        {
            return _productDAL.GetAllProducts().Where(p => p.StockCount <= p.LowStockThreshold).ToList();
        }
    }
}
