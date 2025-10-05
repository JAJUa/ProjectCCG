using UnityEngine;

namespace Systems.Base
{
    /// <summary>
    /// 타입의 객체를 싱글톤화 하는 추상 클래스
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
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