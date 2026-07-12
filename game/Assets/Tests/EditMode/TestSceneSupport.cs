using System.Linq;
using System.Reflection;
using ClientIsKing.EditorTools;
using ClientIsKing.Economy;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-110 U6: EditMode 배치 실행에서는 EditorSceneManager.OpenScene 이 씬 오브젝트를 로드해도
    /// MonoBehaviour.Awake() 가 동기 호출된다는 보장이 없다(씬 로드/AddComponent 공통 함정 — task-110
    /// group1 troubleshooting 참조). production 코드(GameManager/ServiceManager/EconomyManager/
    /// SettlementManager)는 정상 Play/씬 로드에서는 문제 없이 동작하며 이 우회는 배치 EditMode 테스트에만 쓴다.
    /// </summary>
    internal static class TestSceneSupport
    {
        /// <summary>Shop 씬을 (재)빌드해 열고, 4개 매니저 singleton Instance 를 실제 씬 컴포넌트로 강제 동기화한다.</summary>
        internal static GameObject OpenShopSceneWithLiveSingletons()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
            var gameManagerGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "GameManager");
            Assert.IsNotNull(gameManagerGo, "GameManager 오브젝트 누락");

            ForceInstance(typeof(GameManager), gameManagerGo.GetComponent<GameManager>());
            ForceInstance(typeof(ServiceManager), gameManagerGo.GetComponent<ServiceManager>());
            ForceInstance(typeof(EconomyManager), gameManagerGo.GetComponent<EconomyManager>());
            ForceInstance(typeof(SettlementManager), gameManagerGo.GetComponent<SettlementManager>());
            return gameManagerGo;
        }

        /// <summary>
        /// MainMenu 씬을 (재)빌드해 열고, 매니저 singleton Instance 를 강제 동기화한다 (task-113 U5 —
        /// MainMenuController.RefreshSaveUi/이어하기 흐름 검증용). Start() 가 배치 EditMode 에서 동기
        /// 호출된다는 보장이 없으므로 호출자가 필요 시 <see cref="ForceStart"/> 로 재확인한다.
        /// </summary>
        internal static GameObject OpenMainMenuSceneWithLiveSingletons()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.MainMenuPath, OpenSceneMode.Single);
            var gameManagerGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "GameManager");
            Assert.IsNotNull(gameManagerGo, "GameManager 오브젝트 누락 (MainMenu)");

            ForceInstance(typeof(GameManager), gameManagerGo.GetComponent<GameManager>());
            ForceInstance(typeof(ServiceManager), gameManagerGo.GetComponent<ServiceManager>());
            ForceInstance(typeof(EconomyManager), gameManagerGo.GetComponent<EconomyManager>());
            ForceInstance(typeof(SettlementManager), gameManagerGo.GetComponent<SettlementManager>());
            return gameManagerGo;
        }

        /// <summary>private static Instance setter 를 리플렉션으로 강제한다 (production API 는 바꾸지 않는다).</summary>
        internal static void ForceInstance(System.Type managerType, object instance)
        {
            var prop = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(prop, $"{managerType.Name}.Instance 프로퍼티를 찾을 수 없습니다.");
            if (ReferenceEquals(prop.GetValue(null), instance))
            {
                return;
            }
            prop.SetValue(null, instance, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
        }

        /// <summary>
        /// private MonoBehaviour.Awake() 를 리플렉션으로 직접 호출한다 — 배치 EditMode 에서는 OpenScene
        /// 이후 Awake() 가 동기 호출된다는 보장이 없어(공통 함정), onClick.AddListener 같은 Awake 전용
        /// 배선이 누락된 채로 남을 수 있다(task-113 U5 — MainMenuController.OnStartClicked/OnContinueClicked
        /// 리스너 미등록으로 클릭이 무시되는 증상 재현). production 코드는 바꾸지 않는다.
        /// </summary>
        internal static void ForceAwake(MonoBehaviour behaviour)
        {
            ForceInvoke(behaviour, "Awake");
        }

        /// <summary>
        /// private MonoBehaviour.Start() 를 리플렉션으로 직접 호출한다 — 배치 EditMode 에서는 Unity 가
        /// OpenScene 직후 Start() 를 동기 호출한다는 보장이 없어(Awake 와 같은 함정), controller 의 초기
        /// BuildKindList/RefreshAll 등이 실행되지 않은 채로 남을 수 있다. production 코드는 바꾸지 않는다.
        /// </summary>
        internal static void ForceStart(MonoBehaviour behaviour)
        {
            ForceInvoke(behaviour, "Start");
        }

        /// <summary>
        /// private MonoBehaviour.OnEnable() 을 리플렉션으로 직접 호출한다 — SetActive 토글이 배치
        /// EditMode 에서 OnEnable 을 동기 호출한다는 보장까지는 없을 수 있어(Awake/Start 와 같은 함정
        /// 계열) 직접 호출로 onClick 리스너 등록·초기 refresh 를 확실히 한다.
        /// </summary>
        internal static void ForceOnEnable(MonoBehaviour behaviour)
        {
            ForceInvoke(behaviour, "OnEnable");
        }

        static void ForceInvoke(MonoBehaviour behaviour, string methodName)
        {
            var method = behaviour.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(behaviour, null);
            }
        }
    }
}
