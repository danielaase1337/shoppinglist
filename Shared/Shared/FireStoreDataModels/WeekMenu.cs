using Google.Cloud.Firestore;
using Shared.BaseModels;
using System;
using System.Collections.Generic;

namespace Shared.FireStoreDataModels
{
    [FirestoreData]
    public class WeekMenu : EntityBase
    {
        [FirestoreProperty]
        public int WeekNumber { get; set; }

        [FirestoreProperty]
        public int Year { get; set; }

        [FirestoreProperty]
        public DateTime PlanningStartDate { get; set; }

        [FirestoreProperty]
        public ICollection<DailyMeal> DailyMeals { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }

        public WeekMenu()
        {
            DailyMeals = new List<DailyMeal>();
            IsActive = true;
        }
    }
}

