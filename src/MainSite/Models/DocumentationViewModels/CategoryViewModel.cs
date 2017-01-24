#region Usings

using System.Collections.Generic;

#endregion

namespace MainSite.Models.DocumentationViewModels
{
    public class CategoryViewModel
    {
        public CategoryViewModel(List<Category> categoriesList)
        {
            CategoriesList = categoriesList;
        }

        public CategoryViewModel(Category category)
        {
            CategoriesList.Add(category);
        }

        public CategoryViewModel()
        {
        }

        public List<Category> CategoriesList { get; set; } = new List<Category>();
        public string Id { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
    }
}
