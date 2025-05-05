using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Mapping {
    public class GenericMapper<TModel, TView> : IModelMapper<TModel, TView> {
        public TView MapToEdit(TModel model) {
            throw new NotImplementedException();
        }

        public List<TView> MapToEdit(IEnumerable<TModel> models) {
            throw new NotImplementedException();
        }

        public TModel MapToModel(TView modelEVM) {
            throw new NotImplementedException();
        }
    }
}
