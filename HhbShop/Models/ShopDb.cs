using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace HhbShop.Models
{
    public sealed class ShopDb : DbContext
    {
        public ShopDb() : base("name=ShopDb") => Configuration.LazyLoadingEnabled = false;
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        protected override void OnModelCreating(DbModelBuilder builder)
        {
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
            builder.Entity<OrderDetail>().Property(o => o.Price).HasPrecision(18, 2);
            builder.Entity<Product>().HasRequired(p => p.Category).WithMany()
                .HasForeignKey(p => p.CategoryId).WillCascadeOnDelete(false);
            builder.Entity<OrderDetail>().HasRequired(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductId).WillCascadeOnDelete(false);
        }
    }

    [Table("Category")]
    public class Category
    {
        public int Id { get; set; }
        [Column("CatName"), Required, StringLength(80), Display(Name = "Tên danh mục")]
        public string Name { get; set; }
    }

    [Table("Product")]
    public class Product
    {
        [Key, Column("ProId")] public int Id { get; set; }
        [Column("ProName")] public string Name { get; set; }
        [Column("ProImage")] public string Image { get; set; }
        [Column("ProPrice")] public decimal Price { get; set; }
        [Column("CatId")] public int CategoryId { get; set; }
        public Category Category { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public bool Featured { get; set; }
        [Timestamp] public byte[] Version { get; set; }
        [NotMapped] public string ImagePath => Image.StartsWith("upload-")
            ? "~/Uploads/" + Image : "~/Content/images/Pro_Images/" + Image;
    }

    [Table("Order")]
    public class Order
    {
        public int Id { get; set; }
        public Guid CheckoutToken { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string Note { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        [Timestamp] public byte[] Version { get; set; }
        public ICollection<OrderDetail> Lines { get; set; } = new List<OrderDetail>();
    }

    [Table("OrderDetail")]
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    [Table("AdminUser")]
    public class AdminUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
    }

    public static class ShopText
    {
        public static string Money(decimal value) => value.ToString("#,0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
        public static string Time(DateTime utc) => utc.AddHours(7).ToString("HH:mm dd/MM/yyyy");
        public static string OrderCode(int id) => "HHB-" + id.ToString("D5");
        public static readonly IReadOnlyDictionary<string, string> Statuses = new Dictionary<string, string>
        {
            ["Pending"] = "Chờ xác nhận", ["Processing"] = "Đang xử lý", ["Completed"] = "Hoàn tất", ["Cancelled"] = "Đã hủy"
        };
        public static IEnumerable<string> NextStatuses(string status)
        {
            switch (status)
            {
                case "Pending": return new[] { "Processing", "Cancelled" };
                case "Processing": return new[] { "Completed", "Cancelled" };
                default: return Enumerable.Empty<string>();
            }
        }
    }
}
