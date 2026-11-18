using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HhbShop.Models
{
    public class HomePage
    {
        public IReadOnlyList<Category> Categories { get; set; }
        public IReadOnlyList<Product> Featured { get; set; }
    }

    public class CatalogPage
    {
        public IReadOnlyList<Category> Categories { get; set; }
        public IReadOnlyList<Product> Products { get; set; }
        public int? CategoryId { get; set; }
        public string Query { get; set; }
        public string Sort { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
        public int Total { get; set; }
    }

    public class DetailsPage
    {
        public Product Product { get; set; }
        public IReadOnlyList<Product> Related { get; set; }
    }

    public class CartLine
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public int Id { get; set; }
        public bool Available => Product != null && Product.IsActive && Product.Stock >= Quantity;
        public decimal Total => Product == null ? 0 : Product.Price * Quantity;
    }

    public class CartPage
    {
        public IReadOnlyList<CartLine> Lines { get; set; }
        public decimal Total => Lines.Sum(x => x.Total);
        public bool CanCheckout => Lines.Count > 0 && Lines.All(x => x.Available);
    }

    public class CheckoutInput
    {
        [Required(ErrorMessage = "Nhập họ và tên."), StringLength(100), Display(Name = "Họ và tên")]
        public string CustomerName { get; set; }
        [Required(ErrorMessage = "Nhập số điện thoại."), RegularExpression(@"^(\+84|0)[0-9]{9,10}$", ErrorMessage = "Nhập số điện thoại hợp lệ, không có dấu cách."), Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; }
        [EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(150), Display(Name = "Email (không bắt buộc)")]
        public string CustomerEmail { get; set; }
        [Required(ErrorMessage = "Nhập địa chỉ giao hàng."), StringLength(250), Display(Name = "Địa chỉ giao hàng")]
        public string CustomerAddress { get; set; }
        [StringLength(350), Display(Name = "Ghi chú (không bắt buộc)")]
        public string Note { get; set; }
        [Required, RegularExpression("^(COD|Simulation)$", ErrorMessage = "Chọn phương thức thanh toán hợp lệ.")]
        public string PaymentMethod { get; set; }
        public Guid Token { get; set; }
    }

    public class ProductInput
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Nhập tên sản phẩm."), StringLength(120), Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }
        [Range(typeof(decimal), "1000", "100000000", ErrorMessage = "Giá phải từ 1.000 đến 100.000.000 ₫."), Display(Name = "Giá bán (₫)")]
        public decimal Price { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Chọn danh mục."), Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Nhập mô tả sản phẩm."), StringLength(1000), Display(Name = "Mô tả")]
        public string Description { get; set; }
        [Range(0, 100000), Display(Name = "Tồn kho")]
        public int Stock { get; set; }
        [Display(Name = "Đang hiển thị")] public bool IsActive { get; set; }
        [Display(Name = "Nổi bật trên trang chủ")] public bool Featured { get; set; }
        public string ImagePath { get; set; }
        public string Version { get; set; }
    }

    public class LoginInput
    {
        [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(80), Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(256), Display(Name = "Mật khẩu")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class CustomerLoginInput
    {
        [Required(ErrorMessage = "Nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(150)]
        public string Email { get; set; }
        [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(256), Display(Name = "Mật khẩu")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class RegisterInput : CustomerLoginInput
    {
        [Required(ErrorMessage = "Nhập họ và tên."), StringLength(100), Display(Name = "Họ và tên")]
        public string Name { get; set; }
        [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
        [Compare("Password", ErrorMessage = "Hai mật khẩu chưa khớp."), Display(Name = "Nhập lại mật khẩu")]
        [Required(ErrorMessage = "Nhập lại mật khẩu."), StringLength(256)]
        public string ConfirmPassword { get; set; }
    }

    public class DashboardPage
    {
        public int Products { get; set; }
        public int Orders { get; set; }
        public int Pending { get; set; }
        public decimal Revenue { get; set; }
        public IReadOnlyList<Order> Recent { get; set; }
    }
}
