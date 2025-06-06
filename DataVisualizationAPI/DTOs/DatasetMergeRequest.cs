using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class DatasetMergeRequest
    {
        [Required]
        public string NewDatasetName { get; set; }

        [Required]
        public int LeftDatasetId { get; set; }

        [Required]
        public int RightDatasetId { get; set; }

        [Required]
        public List<MergeJoinCondition> JoinConditions { get; set; }

        public string MergeType { get; set; } = "Inner";  // Inner, Left, Right, Full, Cross
    }

    public class MergeJoinCondition
    {
        [Required]
        public string LeftColumn { get; set; }

        [Required]
        public string RightColumn { get; set; }

        public string Operator { get; set; } = "eq";  // eq, neq, gt, gte, lt, lte
    }
} 