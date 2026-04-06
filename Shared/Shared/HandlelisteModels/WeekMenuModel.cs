using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.HandlelisteModels
{
    public class WeekMenuModel : EntityBase
    {
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public DateTime PlanningStartDate { get; set; }
        public ICollection<DailyMealModel> DailyMeals { get; set; }
        public bool IsActive { get; set; }

        public WeekMenuModel()
        {
            DailyMeals = new List<DailyMealModel>();
            IsActive = true;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && WeekNumber > 0 && Year > 0;
        }
    }
}

