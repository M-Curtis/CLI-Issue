using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MainSite.Data
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequestFormSizeLimitAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
    {
            private readonly FormOptions _formOptions;

            public RequestFormSizeLimitAttribute(int valueLengthLimit)
            {
                _formOptions = new FormOptions()
                {
                    ValueLengthLimit = valueLengthLimit
                };
            }

            public int Order { get; set; }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                var features = context.HttpContext.Features;
                var formFeature = features.Get<IFormFeature>();

                if (formFeature?.Form == null)
                {
                    // Request form has not been read yet, so set the limits
                    features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, _formOptions));
                }
            }
        }
}
