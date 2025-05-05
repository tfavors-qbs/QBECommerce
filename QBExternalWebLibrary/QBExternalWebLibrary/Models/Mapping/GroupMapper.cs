using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Products;

namespace QBExternalWebLibrary.Models.Mapping {
    public class GroupMapper : IModelMapper<Group, GroupEditViewModel> {
        private readonly IRepository<Group> _groupRepository;
        private readonly IRepository<Class> _classRepository;

        public GroupMapper(IRepository<Group> groupRepository, IRepository<Class> classRepository) {
            _groupRepository = groupRepository;
            _classRepository = classRepository;
        }

        public GroupEditViewModel MapToEdit(Group model) {
            return new GroupEditViewModel {
                Id = model.Id,
                Name = model.Name,
                DisplayName = model.DisplayName,
                LegacyId = model.LegacyId,
                ClassId = model.ClassId,
                Description = model.Description,
            };
        }

        public List<GroupEditViewModel> MapToEdit(IEnumerable<Group> models) {
            throw new NotImplementedException();
        }

        public Group MapToModel(GroupEditViewModel view) {
            var group = _groupRepository.GetById(view.Id);
            if (group == null) {
                group = new Group {
                    Id = view.Id,
                    Name = view.Name,
                    DisplayName = view.DisplayName,
                    LegacyId = view.LegacyId,
                    ClassId = view.ClassId,
                    Description = view.Description,
                    Class = _classRepository.GetById(view.ClassId)
                };
            } else {
                group.Id = view.Id;
                group.Name = view.Name;
                group.DisplayName = view.DisplayName;
                group.LegacyId = view.LegacyId;
                group.ClassId = view.ClassId;
                group.Description = view.Description;
                group.Class = _classRepository.GetById(view.ClassId);
            }
            return group;
        }
    }
}
