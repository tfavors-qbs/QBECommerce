using QBExternalWebLibrary.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Mapping {
    public class ClientMapper : IModelMapper<Client, ClientEditViewModel> {
        private readonly IRepository<Client> _repository;

        public ClientMapper(IRepository<Client> repository) {
            _repository = repository;
        }

        public Client MapToModel(ClientEditViewModel view) {
            var client = _repository.GetById(view.Id);
            if (client == null) {
                client = new Client {
                    Id = view.Id,
                    Name = view.Name,
                    LegacyId = view.LegacyId,
                };
            } else {
                client.Name = view.Name;
                client.LegacyId = view.LegacyId;
            }
            return client;
        }

        public ClientEditViewModel MapToEdit(Client model) {
            return new ClientEditViewModel {
                Id = model.Id,
                LegacyId = model.LegacyId,
                Name = model.Name,
            };
        }

        public List<ClientEditViewModel> MapToEdit(List<ClientEditViewModel> list) {
            throw new NotImplementedException();
        }

        public List<ClientEditViewModel> MapToEdit(IEnumerable<Client> models) {
            throw new NotImplementedException();
        }
    }
}
