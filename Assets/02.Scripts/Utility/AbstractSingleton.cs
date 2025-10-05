using UnityEngine;

namespace Systems.Base
{
    /// <summary>
    /// Ÿ���� ��ü�� �̱���ȭ �ϴ� �߻� Ŭ����
    /// </summary>
    /// <typeparam name="T">������Ʈ Ÿ��</typeparam>
    public abstract class AbstractSingleton<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
            }
        }
    }
}