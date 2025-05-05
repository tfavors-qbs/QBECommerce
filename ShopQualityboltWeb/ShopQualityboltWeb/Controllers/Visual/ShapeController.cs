using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class ShapeController : Controller
    {
        private readonly IModelService<Shape, Shape> _shapeService;

        public ShapeController(IModelService<Shape, Shape> shapeService)
        {
            _shapeService = shapeService;
        }

        // GET: Shape
        public async Task<IActionResult> Index()
        {
            return View(_shapeService.GetAll());
        }

        // GET: Shape/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shape = _shapeService.GetById((int)id);
            if (shape == null)
            {
                return NotFound();
            }

            return View(shape);
        }

        // GET: Shape/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Shape/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DisplayName,Description")] Shape shape)
        {
            if (ModelState.IsValid)
            {
                _shapeService.Create(shape);
                return RedirectToAction(nameof(Index));
            }
            return View(shape);
        }

        // GET: Shape/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shape = _shapeService.GetById((int)id);
            if (shape == null)
            {
                return NotFound();
            }
            return View(shape);
        }

        // POST: Shape/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DisplayName,Description")] Shape shape)
        {
            if (id != shape.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _shapeService.Update(shape);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShapeExists(shape.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(shape);
        }

        // GET: Shape/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shape = _shapeService.GetById((int)id);
            if (shape == null)
            {
                return NotFound();
            }

            return View(shape);
        }

        // POST: Shape/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shape = _shapeService.GetById((int)id);
            if (shape != null)
            {
                _shapeService.Delete(shape);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ShapeExists(int id)
        {
            return _shapeService.Exists(e => e.Id == id);
        }
    }
}
