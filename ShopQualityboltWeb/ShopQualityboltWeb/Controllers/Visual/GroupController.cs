using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class GroupController : Controller {
        private readonly IModelService<Group, GroupEditViewModel> _groupService;
        private readonly IModelService<Class, Class?> _classService;

        public GroupController(IModelService<Group, GroupEditViewModel> groupService, IModelService<Class, Class?> classEditService) {
            _groupService = groupService;
            _classService = classEditService;
        }

        // GET: Group
        public async Task<IActionResult> Index() {
            var groups = _groupService.GetAll().ToList();
            return View(groups);
        }

        // GET: Group/Details/5
        public async Task<IActionResult> Details(int? id) {
            if (id == null) {
                return NotFound();
            }

            var @group = _groupService.GetById(id);

            if (@group == null) {
                return NotFound();
            }

            return View(@group);
        }

        // GET: Group/Create
        public IActionResult Create() {
            ViewData["ClassId"] = new SelectList(_classService.GetAll(), "Id", "DisplayName");
            return View();
        }

        // POST: Group/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,LegacyId,Name,DisplayName,Description,ClassId")] GroupEditViewModel @groupEditViewModel) {
            
            if (ModelState.IsValid) {
                _groupService.Create(null, groupEditViewModel);               
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_classService.GetAll(), "Id", "DisplayName", @groupEditViewModel.ClassId);
            return View(@groupEditViewModel);
        }

        // GET: Group/Edit/5
        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return NotFound();
            }

            var group = _groupService.GetById(id);
            var groupEditViewModel = _groupService.GetView(group);

            if (group == null) {
                return NotFound();
            }
            ViewData["ClassId"] = new SelectList(_classService.GetAll(), "Id", "DisplayName", @group.ClassId);
            return View(@groupEditViewModel);
        }

        // POST: Group/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LegacyId,Name,DisplayName,Description,ClassId,Class")] GroupEditViewModel groupEditViewModel) {
            var group = _groupService.GetById(id);
            if (group == null) {
                return NotFound();
            }

            if (ModelState.IsValid) {
                try {
                    _groupService.Update(null, groupEditViewModel);
                } catch (DbUpdateConcurrencyException) {
                    if (!GroupExists(@group.Id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_classService.GetAll(), "Id", "DisplayName", @group.ClassId);
            return View(@group);
        }

        // GET: Group/Delete/5
        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }

            var @group = _groupService.GetById(id);

            if (@group == null) {
                return NotFound();
            }

            return View(@group);
        }

        // POST: Group/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            var @group = _groupService.GetById(id);
            if (@group != null) {
                _groupService.Delete(@group);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id) {
            return _groupService.Exists(e => e.Id == id);
        }
    }
}
