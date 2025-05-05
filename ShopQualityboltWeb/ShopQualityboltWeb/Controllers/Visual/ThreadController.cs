using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Services.Model;
using Thread = QBExternalWebLibrary.Models.Products.Thread;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class ThreadController : Controller
    {
        private readonly IModelService<Thread, Thread> _service;

        public ThreadController(IModelService<Thread, Thread> service)
        {
            _service = service;
        }

        // GET: Thread
        public async Task<IActionResult> Index()
        {
            return View(_service.GetAll());
        }

        // GET: Thread/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thread = _service.GetById((int)id);
            
            if (thread == null)
            {
                return NotFound();
            }

            return View(thread);
        }

        // GET: Thread/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Thread/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DisplayName,Description")] Thread thread)
        {
            if (ModelState.IsValid)
            {
                _service.Create(thread);
                return RedirectToAction(nameof(Index));
            }
            return View(thread);
        }

        // GET: Thread/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thread = _service.GetById((int)id);
            if (thread == null)
            {
                return NotFound();
            }
            return View(thread);
        }

        // POST: Thread/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DisplayName,Description")] Thread thread)
        {
            if (id != thread.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _service.Update(thread);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThreadExists(thread.Id))
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
            return View(thread);
        }

        // GET: Thread/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thread = _service.GetById((int)id);

            if (thread == null)
            {
                return NotFound();
            }

            return View(thread);
        }

        // POST: Thread/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thread = _service.GetById(id);
            if (thread != null)
            {
                _service.Delete(thread);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ThreadExists(int id)
        {
            return _service.Exists(e => e.Id == id);
        }
    }
}
