namespace MemmoApi.Models
{
    public enum SettingsParentType
    {
        Status = 1,
        Project = 2
    }

    public enum StatusOption
    {
        Todo = 101,
        InProgress = 102,
        Done = 103,
        Blocked = 104
    }

    public enum ProjectOption
    {
        Internal = 201,
        Client = 202,
        Maintenance = 203,
        Research = 204
    }
}
