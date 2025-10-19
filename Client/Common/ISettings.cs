using Shared.FireStoreDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorApp.Client.Common
{
    public  interface ISettings
    {
        string GetApiUrl(ShoppingListKeysEnum key);
        string GetApiUrlId(ShoppingListKeysEnum key, object id);
    }
    public class Settings : ISettings
    {
        private Dictionary<string, string> _apiUrl;
        public Settings()
        {
            _apiUrl = new Dictionary<string, string>
            {
                { "shoppinglist", "api/shoppinglist" },
                { "shoppinglists", "api/shoppinglists" },
                { "shop", "api/shop" },
                { "shops", "api/shops" },
                { "shopitem", "api/shopitem" },
                { "shopitems", "api/shopitems" },
                { "itemcategory","api/itemcategory" },
                { "itemcategorys", "api/itemcategorys" },
                { "frequentshoppinglist", "api/frequentshoppinglist" },
                { "frequentshoppinglists", "api/frequentshoppinglists" }
            };
        }
        public string GetApiUrl(ShoppingListKeysEnum key)
        {
            if (string.IsNullOrEmpty(key.ToString())) return string.Empty;
            
            var result = _apiUrl.TryGetValue(key.ToString().ToLowerInvariant(), out string res);
            if (result)
                return res;
            else return string.Empty;
        }

        public string GetApiUrlId(ShoppingListKeysEnum key, object id)
        {
            var part1 = GetApiUrl(key);
            return $"{part1}/{id.ToString()}";
        }
    }


}
