using System.Reflection;

namespace Santa.Tests
{
    /// <summary>
    /// テストからScriptableObjectの private [SerializeField] を埋めるためのヘルパー。
    /// PlayMode テストは Standalone Player 上でも走れることが期待されるため、
    /// UnityEditor.SerializedObject には依存せず素朴なリフレクションで済ませる。
    /// </summary>
    public static class ReflectionTestUtil
    {
        public static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (field == null)
            {
                throw new System.MissingFieldException($"フィールドが見つかりません: {target.GetType().Name}.{fieldName}");
            }

            field.SetValue(target, value);
        }

        public static T GetPrivateField<T>(object target, string fieldName)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (field == null)
            {
                throw new System.MissingFieldException($"フィールドが見つかりません: {target.GetType().Name}.{fieldName}");
            }

            return (T)field.GetValue(target);
        }
    }
}
