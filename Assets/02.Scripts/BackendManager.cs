/*using UnityEngine;
using BackEnd;

public class BackendManager : MonoBehaviour
{
    void Start()
    {
        var bro = Backend.Initialize(); // �ڳ� �ʱ�ȭ

        // �ڳ� �ʱ�ȭ�� ���� ���䰪
        if (bro.IsSuccess())
        {
            Debug.Log("�ʱ�ȭ ���� : " + bro); // ������ ��� statusCode 204 Success
        }
        else
        {
            Debug.LogError("�ʱ�ȭ ���� : " + bro); // ������ ��� statusCode 400�� ���� �߻�
        }
    }

    [ContextMenu("SignUp")]
    void Test1()
    {
        BackendLogin.Instance.CustomSignUp("user1", "1234"); // [�߰�] �ڳ� ȸ������ �Լ�
        Debug.Log("�׽�Ʈ�� �����մϴ�.");
    }

    [ContextMenu("Login")]
    void Test2()
    {
        BackendLogin.Instance.CustomLogin("user1", "1234"); // [�߰�] �ڳ� �α���
        BackendLogin.Instance.UpdateNickname("���ϴ� �̸�"); // [�߰�] �г��� ����

        // [�߰�] ������ �ҷ��� �����Ͱ� �������� ���� ���, �����͸� ���� �����Ͽ� ����
        if (BackendGameData.userData == null)
        {
            BackendGameData.Instance.GameDataInsert();
        }

        BackendGameData.Instance.GameDataGet(); //[�߰�] ������ ���� �Լ�

        BackendGameData.Instance.LevelUp(); // [�߰�] ���ÿ� ����� �����͸� ����

        BackendGameData.Instance.GameDataUpdate(); //[�߰�] ������ ����� �����͸� �����(����� �κи�)

        Debug.Log("�׽�Ʈ�� �����մϴ�.");
    }

    [ContextMenu("T2")]
    void Test3()
    {
        BackendGameData.Instance.GameDataInsert(); //[�߰�] ������ ���� �Լ�
    }

    [ContextMenu("T")]
    void Test4()
    {
    }
}*/