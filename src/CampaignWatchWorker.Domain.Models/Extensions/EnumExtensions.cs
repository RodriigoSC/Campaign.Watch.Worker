using System.ComponentModel;
using System.Reflection;

namespace CampaignWatchWorker.Domain.Models.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null)
                    return attribute.Description;
            }
            return value.ToString();
        }

        public static T ToEnumByDescription<T>(this string description) where T : Enum
        {
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("A descrição do enum não pode ser nula ou vazia.");
            }

            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                        return (T)field.GetValue(null);
                }

                if (field.Name.Equals(description, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)field.GetValue(null);
                }
            }

            throw new ArgumentException($"A descrição '{description}' não corresponde a nenhum membro do enum {typeof(T).Name}.");
        }
    }
}
