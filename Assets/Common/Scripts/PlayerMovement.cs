using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
  public float speed = 5f;

  private Controls input; // 你的 C# 类名
  private Vector2 moveInput;

  void Awake()
  {
    input = new Controls();
    input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
    input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
  }

  void OnEnable()
  {
    input.Player.Enable();
  }

  void OnDisable()
  {
    input.Player.Disable();
  }

  void Update()
  {
    Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);
    transform.Translate(dir * speed * Time.deltaTime, Space.World);
  }
}
