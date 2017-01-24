namespace MainSite.Models.DocumentationViewModels
{
    public class DocumentViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public DocumentViewModel() { }

        public DocumentViewModel(int id, string title, string category)
        {
            Id = id;
            Title = title;
            Category = category;
        }
    }
}
