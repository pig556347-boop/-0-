using UnityEngine;

namespace SchoolEscape
{
    // Independent flat XZ controller with movement-based stamina. No combat inputs.
    [RequireComponent(typeof(CharacterController))]
    public sealed class Explorer : MonoBehaviour
    {
        public MapData Data;
        public ExplorationGame Game;
        public Transform Visual, LeftArm, RightArm, LeftLeg, RightLeg, LeftKnee, RightKnee;
        public float Speed { get; private set; }
        public float Distance { get; private set; }
        public float Stamina { get; private set; } = 100;
        public float MaximumStamina=100;
        public void SetStamina(float value){Stamina=Mathf.Clamp(value,0,MaximumStamina);Exhausted=Stamina<=0;}
        public bool Exhausted { get; private set; }
        public bool IsRunning { get; private set; }
        public bool CanRun => !Exhausted && Stamina > 0;
        public float SpeedBoostUntil { get; private set; }
        public float SpeedMultiplier => Game!=null&&Game.SimulationTime<SpeedBoostUntil?1.3f:1;
        float staminaDelay;
        float footstepClock;
        public bool InputEnabled = true;
        CharacterController controller;
        float phase;
        public Vector3 LastDelta { get; private set; }
        void Awake() => controller = GetComponent<CharacterController>();
        void Update()
        {
            if (!InputEnabled) return;
            Vector2 axes = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
                                       (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                                       (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) -
                                       (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            Step(axes, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), Mathf.Min(Time.deltaTime, .05f));
        }
        public void Step(Vector2 axes, bool run, float dt)
        {
            if(Game!=null&&Game.Paused){Speed=0;LastDelta=Vector3.zero;IsRunning=false;return;}
            dt = Mathf.Clamp(dt, 0, .05f);
            staminaDelay = Mathf.Max(0, staminaDelay - dt);
            if(Game!=null&&(Game.Opening||Game.Caught||Game.Paused||Game.Won))
            {
                Speed=0;LastDelta=Vector3.zero;IsRunning=false;
                if(!Game.Caught&&!Game.Paused&&!Game.Won)RecoverStamina(dt);
                return;
            }
            axes = Vector2.ClampMagnitude(axes, 1);
            Vector3 direction = new Vector3(axes.x, 0, axes.y);
            run = run && CanRun && axes.sqrMagnitude > .0001f;
            float targetSpeed = (run ? Data.runSpeed : Data.walkSpeed)*SpeedMultiplier;
            Vector3 before = transform.position;
            controller.Move(direction * targetSpeed * dt + Vector3.down * .1f);
            LastDelta = transform.position - before;
            LastDelta = new Vector3(LastDelta.x, 0, LastDelta.z);
            Speed = dt > 0 ? LastDelta.magnitude / dt : 0;
            Distance += LastDelta.magnitude;
            IsRunning = run && Speed > .05f;
            if(Speed>.15f&&Game!=null)
            {
                footstepClock+=dt;float period=IsRunning?.28f:.44f;
                if(footstepClock>=period){footstepClock%=period;Game.EmitNoise(Game.Current.id,Data.Grid(transform.position),IsRunning?8:2,"footstep");}
            }
            else footstepClock=0;
            if(Game?.Native?.KitchenActive==true){RestoreStamina();}
            else if(IsRunning)
            {
                Stamina = Mathf.Max(0, Stamina - 20 * dt);staminaDelay = 1.5f;
                if(Stamina <= .0001f){Stamina=0;Exhausted=true;}
            }
            else RecoverStamina(dt);
            float moving = Mathf.Clamp01(Speed / Data.walkSpeed);
            phase += Speed * dt * (run ? 4.2f : 5.3f);
            if(Game.Current.subjectEvent=="art"){var ray=Game.View.ScreenPointToRay(Input.mousePosition);var plane=new Plane(Vector3.up,Vector3.zero);if(plane.Raycast(ray,out float t)){Vector3 aim=ray.GetPoint(t)-transform.position;aim.y=0;if(aim.sqrMagnitude>.01f)Visual.rotation=Quaternion.LookRotation(aim);}}
            else if (direction.sqrMagnitude > .01f)
                Visual.rotation = Quaternion.Slerp(Visual.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-16 * dt));
            float swing = Mathf.Sin(phase) * (run ? 42 : 28) * moving;
            LeftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
            RightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
            LeftKnee.localRotation = Quaternion.Euler(Mathf.Max(0,swing)*(run?1.2f:.8f),0,0);
            RightKnee.localRotation = Quaternion.Euler(Mathf.Max(0,-swing)*(run?1.2f:.8f),0,0);
            LeftArm.localRotation = Quaternion.Euler(-swing * .8f, 0, run ? -12 : -4);
            RightArm.localRotation = Quaternion.Euler(swing * .8f, 0, run ? 12 : 4);
            Visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(phase)) * .045f * moving, 0);
            if(Game!=null&&Game.AutomaticStairsEnabled)Game.CheckAutomaticStairs();
        }
        void RecoverStamina(float dt)
        {
            if(staminaDelay<=0)Stamina=Mathf.Min(MaximumStamina,Stamina+18*dt);
            if(Exhausted&&Stamina>=MaximumStamina*.25f)Exhausted=false;
        }
        public void ResetState()
        {
            Stamina=MaximumStamina;Exhausted=false;IsRunning=false;staminaDelay=0;
            Speed=0;Distance=0;LastDelta=Vector3.zero;phase=0;footstepClock=0;SpeedBoostUntil=-1;
        }
        public void ApplyVitamin()=>SpeedBoostUntil=(Game!=null?Game.SimulationTime:Time.time)+2;
        public void RestoreStamina(){Stamina=MaximumStamina;Exhausted=false;staminaDelay=0;}
        public void Warp(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position + Vector3.up * .025f;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
