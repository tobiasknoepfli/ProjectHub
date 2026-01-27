using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

namespace ProjectHub.App.Models
{
    [Table("projects")]
    public class Project : BaseModel, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private string _name = string.Empty;
        private string _description = string.Empty;
        private string? _logoUrl;

        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("name")]
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        [Column("description")]
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("logo_url")]
        public string? LogoUrl { get => _logoUrl; set { _logoUrl = value; OnPropertyChanged(); } }

        public override string ToString() => Name;
    }

    [Table("sprints")]
    public class Sprint : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public bool IsCurrent => DateTime.Today >= StartDate.Date && DateTime.Today <= EndDate.Date;
        public bool CanBeCompleted => IsActive && DateTime.Today > EndDate.Date;
        public bool IsPast => !IsActive;
    }

    [Table("issues")]
    public class Issue : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [Column("sprint_id")]
        public Guid? SprintId { get; set; }

        [Column("program_component")]
        public string ProgramComponent { get; set; } = string.Empty;

        [Column("sub_components")]
        public string SubComponents { get; set; } = string.Empty; // Semicolon separated

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = "Bug"; // Bug, Feature, Idea, Story

        [Column("category")]
        public string Category { get; set; } = "Backlog"; // Backlog, Pipeline, Hub

        [Column("status")]
        public string Status { get; set; } = "Open"; // Open, In Progress, In Testing, Finished

        [Column("priority")]
        public int Priority { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Formatted title: Program / Sub1 / Sub2 : Description
        public string FormattedTitle
        {
            get
            {
                var subs = SubComponents.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var subPart = subs.Length > 0 ? " / " + string.Join(" / ", subs) : "";
                return $"{ProgramComponent}{subPart} : {Description}";
            }
        }

        public string AgeString
        {
            get
            {
                var age = DateTime.UtcNow - CreatedAt;
                if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d up";
                if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h up";
                return $"{(int)age.TotalMinutes}m up";
            }
        }
    }

    [Table("issue_logs")]
    public class IssueLog : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("issue_id")]
        public Guid IssueId { get; set; }

        [Column("user_name")]
        public string UserName { get; set; } = "System";

        [Column("action")]
        public string Action { get; set; } = string.Empty; // Created, Edited, Status Changed

        [Column("details")]
        public string Details { get; set; } = string.Empty;

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
