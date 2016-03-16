using System;

// ReSharper disable InconsistentNaming

namespace TheBorg.Plugins.Optimizely.DTO
{
    public class Project
    {
        public string id { get; set; }
        public string account_id { get; set; }
        public int code_revision { get; set; }
        public string project_name { get; set; }
        public string project_status { get; set; }
        public DateTime created { get; set; }
        public DateTime last_modified { get; set; }
        public string library { get; set; }
        public bool include_jquery { get; set; }
        public ulong js_file_size { get; set; }
        public string project_javascript { get; set; }
        public bool enable_force_variation { get; set; }
        public bool exclude_disabled_experiments { get; set; }
        public bool exclude_names { get; set; }
        public bool ip_anonymization { get; set; }
        public string ip_filter { get; set; }
        public string socket_token { get; set; }
        public string dcp_service_id { get; set; }
    }
}