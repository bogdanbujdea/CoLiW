namespace CoLiW
{
    public class PostInfo
    {

        public PostInfo()
        {
            IsDraft = false;
            Title = "New post";
        }

        public string Title { get; set; }

        public string Content { get; set; }

        public bool IsDraft { get; set; }

        public string BlogId { get; set; }

        public bool Parse { get; set; }

        public string[] Args { get; set; }
    }
}