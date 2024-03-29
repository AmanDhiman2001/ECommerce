﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Project_Ecomm_Offfical.Data;
using Project_Ecomm_Offical.DataAccess.Repository;
using Project_Ecomm_Offical.DataAccess.Repository.IRepository;
using Project_Ecomm_Offical.Models;
using Project_Ecomm_Offical.Models.ViewModels;
using Project_Ecomm_Offical.Utility;
using System.Data;

namespace Project_Ecomm_Offical.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        public ProductController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment)
        {                     
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            
        }

        public IActionResult Index()
        {
            return View();
        }
        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var ProductList =_unitOfWork.Product.GetAll(includeProperties:"Category,CoverType");
            return Json (new {data = ProductList});
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var productInDb = _unitOfWork.Product.Get(id);
            if(productInDb==null)
                return Json(new {success= false, message="something went wrong while delete data"});
            //Image Delete
            var webRootPath = _webHostEnvironment.WebRootPath;
            var imagePath = Path.Combine(webRootPath, productInDb.ImageUrl.Trim('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
            _unitOfWork.Product.Remove(productInDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "data delete successfully !!" });
        }
        #endregion
        public IActionResult Upsert(int? id )
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(cl => new SelectListItem()
                {
                    Text = cl.Name,
                    Value = cl.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(cl => new SelectListItem()
                {
                    Text = cl.Name,
                    Value = cl.Id.ToString()
                })

            };
            if (id == null) return View(productVM);
            productVM.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());
           return View(productVM);
            
        }
        public IActionResult ProductDetail()
        {
            List<Product> products = new List<Product>();
            products = _unitOfWork.Product.GetAll().ToList();
            products.Insert(0, new Product {Id=0, Title="Select Product Name" });
            ViewBag.Products = products;
            var Order = _unitOfWork.OrderDetail.GetAll(includeProperties: "OrderHeader,Product");
            return View(Order);
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            if (!ModelState.IsValid)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count() > 0)
                {
                    var fileName = Guid.NewGuid().ToString();
                    var extension = Path.GetExtension(files[0].FileName);
                    var uploads = Path.Combine(webRootPath, @"Images\product");
                    if (productVM.Product.Id != 0)
                    {
                        var imageExists = _unitOfWork.Product.Get(productVM.Product.Id).ImageUrl; //Path of image
                        productVM.Product.ImageUrl = imageExists;
                    }
                    if (productVM.Product.ImageUrl!= null)
                    {
                        var imagePath = Path.Combine(webRootPath,productVM.Product.ImageUrl.Trim('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    // how to image will save
                    using (var fileStream =new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        files[0].CopyTo(fileStream); //save files.
                    }
                    productVM.Product.ImageUrl = @"\Images\product\" + fileName + extension;
                }
                else
                {
                    if (productVM.Product.Id!= null)
                    {
                        var imageExists = _unitOfWork.Product.Get(productVM.Product.Id).ImageUrl;
                        productVM.Product.ImageUrl= imageExists;
                    }
                }                       
                if (productVM.Product.Id==0)
                    _unitOfWork.Product.Add(productVM.Product);
                else
                    _unitOfWork.Product.Update(productVM.Product);
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                productVM = new ProductVM()
                {
                    Product = new Product(),
                    CategoryList = _unitOfWork.Category.GetAll().Select(cl => new SelectListItem()
                    {
                        Text = cl.Name,
                        Value = cl.Id.ToString()
                    }),
                    CoverTypeList = _unitOfWork.CoverType.GetAll().Select(ct => new SelectListItem()
                    {
                        Text = ct.Name,
                        Value = ct.Id.ToString()
                    })
                };
                if(productVM.Product.Id != 0)
                    return View(productVM);//Create
                productVM.Product = _unitOfWork.Product.Get(productVM.Product.Id);
                        return View (productVM);
            }
            
        }
    }
}
