using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using QBExternalWebLibrary.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Mapping {
    public interface IModelMapper<TModel, TView> {
        public TModel MapToModel(TView view);
        public TView MapToEdit(TModel model);
        public List<TView> MapToEdit(IEnumerable<TModel> models);
    }
}
