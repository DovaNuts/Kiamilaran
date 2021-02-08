using InControl;

public class HeroInput : PlayerActionSet
{
    //Keyboard movement
    public PlayerAction left;
    public PlayerAction right;
    public PlayerAction up;
    public PlayerAction down;
    public PlayerTwoAxisAction moveVector;

    //Gamepad movement
    public PlayerAction rs_up;
    public PlayerAction rs_down;
    public PlayerAction rs_left;
    public PlayerAction rs_right;
    public PlayerTwoAxisAction rightStick;

    //Button actions
    public PlayerAction jump;
    public PlayerAction dash;
    public PlayerAction run;
    public PlayerAction attack;

    //UI Actions
    public PlayerAction quickMap;
    public PlayerAction skipText;
    public PlayerAction skipCutscene;
    public PlayerAction openInventory;
    public PlayerAction paneRight;
    public PlayerAction paneLeft;
    public PlayerAction menuSubmit;
    public PlayerAction menuCancel;

    public HeroInput()
    {
        menuSubmit = CreatePlayerAction("Submit");
        menuCancel = CreatePlayerAction("Cancel");

        left = CreatePlayerAction("Left");
        left.StateThreshold = 0.3f;
        right= CreatePlayerAction("Right");
        right.StateThreshold = 0.3f;
        up = CreatePlayerAction("Up");
        up.StateThreshold = 0.5f;
        down = CreatePlayerAction("Down");
        down.StateThreshold = 0.5f;
        moveVector = CreateTwoAxisPlayerAction(left, right, down, up);
        ActiveDevice.LeftStick.LowerDeadZone = 0.15f;
        ActiveDevice.LeftStick.UpperDeadZone = 0.95f;
    }
}