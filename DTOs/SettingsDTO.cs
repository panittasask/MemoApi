namespace MemmoApi.DTOs
{
    public class SettingsResponse
    {
        public List<DropdownParentItem> Parents { get; set; } = new();
        public List<DropdownChildItem> Children { get; set; } = new();
    }

    public class DropdownParentItem
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class DropdownChildItem
    {
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateParentSettingRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateChildSettingRequest
    {
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
