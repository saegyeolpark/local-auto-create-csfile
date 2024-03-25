using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glider.Core.SerializableData
{
    public abstract class StaticDataBase
    {
        private static readonly Dictionary<Type, FieldInfo[]> TypeFieldsCache = new Dictionary<Type, FieldInfo[]>();

        public int GetCrcHash()
        {
            int hash = 17;
            var type = this.GetType();

            // 타입에 대한 필드 목록을 캐시에서 조회합니다.
            if (!TypeFieldsCache.TryGetValue(type, out var fields))
            {
                // 캐시에 없는 경우, 필드 목록을 조회하고 캐시에 추가합니다.
                fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                TypeFieldsCache[type] = fields;
            }

            // 캐시된 필드 목록을 사용하여 해시 코드를 계산합니다.
            foreach (var field in fields)
            {
                var value = field.GetValue(this);
                if (value is Array array)
                {
                    foreach (var element in array)
                    {
                        if (element is StaticDataBase @base)
                        {
                            hash = hash ^ (@base?.GetCrcHash() ?? 0);
                        }
                        else
                        {
                            hash = hash ^ (element?.GetHashCode() ?? 0);
                        }
                    }
                }
                else
                {
                    if (value is StaticDataBase @base)
                    {
                        hash = hash ^ (@base?.GetCrcHash() ?? 0);
                    }
                    else
                    {
                        hash = hash ^ (value?.GetHashCode() ?? 0);
                    }
                }
            }

            return hash;
        }
    }
}