using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LanguagePatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameAssembly = AssemblyDefinition.ReadAssembly("Stardew Valley.exe");

            var injectees = gameAssembly.Modules.SelectMany(mod => ModuleDefinitionRocks.GetAllTypes(mod))
                                                .SelectMany(t => t.Methods)
                                                .Where(method => null != method.Body).ToList();

            InjectSpriteBatchDrawString(gameAssembly, injectees);
            InjectSpriteFontMeasureString(gameAssembly, injectees);


            gameAssembly.Write(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Stardew Valley.exe"));


            StartGame();
        }

        static void InjectSpriteBatchDrawString(AssemblyDefinition gameAssembly, List<MethodDefinition> injectees)
        {
            var FullCallbackMethod = typeof(Localization).GetMethod("SpriteBatchDrawStringCallback", new Type[] { typeof(SpriteBatch),
            typeof(SpriteFont),typeof(string),typeof(Vector2),typeof(Color),typeof(float),typeof(Vector2),typeof(float),typeof(SpriteEffects),typeof(float)});
            var FullCallback = gameAssembly.MainModule.Import(FullCallbackMethod);
            var ShortCallbackMethod = typeof(Localization).GetMethod("SpriteBatchDrawStringCallback", new Type[] { typeof(SpriteBatch),
            typeof(SpriteFont),typeof(string),typeof(Vector2),typeof(Color) });
            var ShortCallback = gameAssembly.MainModule.Import(ShortCallbackMethod);

            int count = 0;
            foreach (var body in injectees.Select(m => m.Body))
            {
                var processor = body.GetILProcessor();
                var instructions = body.Instructions.Where(instr => instr.OpCode == OpCodes.Callvirt && instr.ToString().Contains("SpriteBatch::DrawString")).ToList();
                foreach (var instr in instructions)
                {
                    var meth = instr.Operand as MethodReference;
                    if (meth.Parameters.Count == 9)
                    {
                        var writeInstruction = processor.Create(OpCodes.Call, FullCallback);
                        processor.Replace(instr, writeInstruction);
                    }
                    else if (meth.Parameters.Count == 4)
                    {
                        var writeInstruction = processor.Create(OpCodes.Call, ShortCallback);
                        processor.Replace(instr, writeInstruction);
                    }
                    count++;
                }
            }
        }
        static void InjectSpriteFontMeasureString(AssemblyDefinition gameAssembly, List<MethodDefinition> injectees)
        {
            var CallbackMethod = typeof(Localization).GetMethod("SpriteFontMeasureStringCallback", new Type[] { typeof(SpriteFont), typeof(string) });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);
            foreach (var body in injectees.Select(m => m.Body))
            {
                var processor = body.GetILProcessor();
                var instructions = body.Instructions.Where(instr => instr.OpCode == OpCodes.Callvirt && instr.ToString().Contains("SpriteFont::MeasureString")).ToList();
                foreach (var instr in instructions)
                {
                    var meth = instr.Operand as MethodReference;
                    var writeInstruction = processor.Create(OpCodes.Call, Callback);
                    processor.Replace(instr, writeInstruction);
                }
            }

        }
        static void InjectGameFunctions(AssemblyDefinition gameAssembly)
        {
            //            "OwnerAccessorType": "@StaticContext",
            //"OwnerMethodName": "Window_ClientSizeChanged",
            //"OwnerMethodDesc": "(System.Object,System.EventArgs)System.Void",
            //"CallbackType": "Storm.StardewValley.StaticGameContext",
            //"InstanceCallbackName": "ClientSizeChangedCallback",
            //"InstanceCallbackDesc": "(Storm.StardewValley.Accessor.StaticContextAccessor)Storm.Manipulation.DetourEvent"
            var injector = new CecilEventCallbackInjector(gameAssembly, new EventCallbackParams
            {
                OwnerType = "StardewValley.Game1",
                OwnerMethodName = "Window_ClientSizeChanged",
                OwnerMethodDesc = "(System.Object,System.EventArgs)System.Void",
                CallbackType = "LanguagePatcher.Localization",
                InstanceCallbackName = "ClientSizeChangedCallback",
                InstanceCallbackDesc = "Storm.Manipulation.DetourEvent"
            });



            var injectee = gameAssembly.GetMethod("StardewValley.Game1", "Window_ClientSizeChanged", "(System.Object, System.EventArgs)System.Void");
        }

        static void StartGame()
        {
            var assembly = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Stardew Valley.exe"));
            assembly.EntryPoint.Invoke(null, new object[] { new string[] { } });
        }
    }

    public static class Localization
    {
        public static Config Config { get; private set; }

        public static void SpriteBatchDrawStringCallback(SpriteBatch batch, SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            var result = OnSpriteBatchDrawString(text);
            if (string.IsNullOrEmpty(result))
            {
                batch.DrawString(spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth);
            }
            else
            {
                batch.DrawString(spriteFont, result, position, color, rotation, origin, scale, effects, layerDepth);
            }
        }
        public static void SpriteBatchDrawStringCallback(SpriteBatch batch, SpriteFont spriteFont, string text, Vector2 position, Color color)
        {
            var result = OnSpriteBatchDrawString(text);
            if (string.IsNullOrEmpty(result))
            {
                batch.DrawString(spriteFont, text, position, color);
            }
            else
            {
                batch.DrawString(spriteFont, result, position, color);
            }
        }
        public static Vector2 SpriteFontMeasureStringCallback(SpriteFont spriteFont, string text)
        {
            var result = OnSpriteFontMeasureString(text);
            if (string.IsNullOrEmpty(result))
            {
                return spriteFont.MeasureString(text);
            }
            else
            {
                return spriteFont.MeasureString(result);
            }
        }
        public static DetourEvent ClientSizeChangedCallback()
        {
            //if (_isMenuDrawing)
            //    _isMenuDrawing = false;
            return new DetourEvent();
        }

        private static string OnSpriteBatchDrawString(string Message)
        {
            //if (ModConfig.LanguageName != "EN")
            //{
            var translateMessage = Translate(Message);
            if (!string.IsNullOrEmpty(translateMessage))
            {
                if (translateMessage.Contains("^"))
                {
                    if (Game1.player.IsMale)
                    {
                        translateMessage = translateMessage.Split('^')[0];
                    }
                    else translateMessage = translateMessage.Split('^')[1];
                }
                return translateMessage;
            }
            //}
            return "";
        }
        private static string OnSpriteFontMeasureString(string Message)
        {
            //if (ModConfig.LanguageName != "EN")
            //{
            var translateMessage = Translate(Message);
            if (!string.IsNullOrEmpty(translateMessage))
            {
                if (translateMessage.Contains("^"))
                {
                    if (Game1.player.IsMale)
                    {
                        translateMessage = translateMessage.Split('^')[0];
                    }
                    else translateMessage = translateMessage.Split('^')[1];
                }
                return translateMessage;
            }
            //}
            return "";
        }

        private static string Translate(string message)
        {
            return "";
        }
    }

    public class CecilEventCallbackInjector
    {
        private readonly AssemblyDefinition def;

        private readonly List<Instruction> injectionPoints = new List<Instruction>();
        private readonly EventCallbackParams @params;
        private readonly AssemblyDefinition self;
        private MethodReference callback;
        private MethodDefinition injectee;
        private bool invalid;

        public CecilEventCallbackInjector(AssemblyDefinition def, EventCallbackParams @params)
        {
            this.self = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
            this.def = def;
            this.@params = @params;
        }

        public void Init()
        {
            injectee = def.GetMethod(@params.OwnerType, @params.OwnerMethodName, @params.OwnerMethodDesc);
            if (injectee == null)
            {
                invalid = true;
                return;
            }

            MethodDefinition recv = null;
            if (injectee.IsStatic) recv = self.GetMethod(@params.CallbackType, @params.StaticCallbackName, @params.StaticCallbackDesc);
            else recv = self.GetMethod(@params.CallbackType, @params.InstanceCallbackName, @params.InstanceCallbackDesc);
            if (recv == null)
            {
                invalid = true;
                return;
            }

            var paramCount = injectee.IsStatic ? 0 : 1;
            if (@params.PushParams)
            {
                paramCount += injectee.Parameters.Count;
            }

            callback = injectee.Module.Import(recv);
            if (paramCount != callback.Parameters.Count)
            {
                invalid = true;
                return;
            }

            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            if (@params.InsertionType == InsertionType.BEGINNING)
            {
                injectionPoints.Add(injecteeInstructions[0]);
                return;
            }

            if (@params.InsertionType == InsertionType.LAST && @params.InsertionIndex == null)
            {
                injectionPoints.Add(injecteeInstructions[injecteeInsCount - 1]);
                return;
            }

            foreach (var i in @params.InsertionIndex)
            {
                switch (@params.InsertionType)
                {
                    case InsertionType.ABSOLUTE:
                        if (i < 0 || i >= injecteeInsCount)
                        {
                            invalid = true;
                            return;
                        }

                        injectionPoints.Add(injecteeInstructions[i]);
                        break;
                    case InsertionType.LAST:
                        if ((injecteeInsCount - 1 - i) < 0 || (injecteeInsCount - 1 - i) >= injecteeInsCount)
                        {
                            invalid = true;
                            return;
                        }

                        injectionPoints.Add(injecteeInstructions[injecteeInsCount - 1 - i]);
                        break;
                    case InsertionType.RETURNS:
                        var relative = GetReturnByRelativity(injectee, i);
                        if (relative == null)
                        {
                            invalid = true;
                            return;
                        }
                        injectionPoints.Add(relative);
                        break;
                }
            }
        }

        public void Inject()
        {
            if (invalid) return;

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = def.MainModule.Import(hasReturnValue.GetMethod);

            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = def.MainModule.Import(eventReturnValue.GetMethod);

            var body = injectee.Body;
            var processor = body.GetILProcessor();

            var returnName = injectee.ReturnType.FullName;
            var returnsVoid = returnName.Equals(typeof(void).FullName);

            var returnsPrimitive = CecilUtils.IsNativeType(returnName);

            foreach (var injectionPoint in injectionPoints)
            {
                var jmpTarget = returnsVoid ? injectionPoint : processor.Create(OpCodes.Pop);

                Instruction initial = null;
                if (!injectee.IsStatic)
                {
                    processor.InsertBefore(injectionPoint, initial = processor.Create(OpCodes.Ldarg_0));
                }

                if (@params.PushParams)
                {
                    for (var i = 0; i < injectee.Parameters.Count(); i++)
                    {
                        switch (i)
                        {
                            case 0:
                                {
                                    var ins = processor.Create(injectee.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                                    if (initial == null) initial = ins;
                                    processor.InsertBefore(injectionPoint, ins);
                                }
                                break;

                            case 1:
                                {
                                    var ins = processor.Create(injectee.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2);
                                    if (initial == null) initial = ins;
                                    processor.InsertBefore(injectionPoint, ins);
                                }
                                break;

                            case 2:
                                {
                                    var ins = processor.Create(injectee.IsStatic ? OpCodes.Ldarg_2 : OpCodes.Ldarg_3);
                                    if (initial == null) initial = ins;
                                    processor.InsertBefore(injectionPoint, ins);
                                }
                                break;

                            case 3:
                                {
                                    var ins = injectee.IsStatic ? processor.Create(OpCodes.Ldarg_3) : processor.Create(OpCodes.Ldarg, i + (injectee.IsStatic ? 0 : 1));
                                    if (initial == null) initial = ins;
                                    processor.InsertBefore(injectionPoint, ins);
                                }
                                break;

                            default:
                                {
                                    var ins = processor.Create(OpCodes.Ldarg, i + (injectee.IsStatic ? 0 : 1));
                                    if (initial == null) initial = ins;
                                    processor.InsertBefore(injectionPoint, ins);
                                }
                                break;
                        }
                    }
                }

                var callbackCall = processor.Create(OpCodes.Call, callback);
                if (initial == null) initial = callbackCall;

                processor.InsertBefore(injectionPoint, callbackCall);
                if (!returnsVoid)
                {
                    processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
                }
                processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));

                var continueNormalJump = processor.Create(OpCodes.Brfalse, jmpTarget);
                processor.InsertBefore(injectionPoint, continueNormalJump);

                if (!returnsVoid)
                {
                    processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
                }

                if (returnsPrimitive)
                {
                    processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Unbox_Any, injectee.ReturnType));
                }

                processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
                if (!returnsVoid)
                {
                    processor.InsertBefore(injectionPoint, jmpTarget);
                }

                if (@params.JumpFix)
                {
                    foreach (var instruction in body.Instructions.Where(i => i != continueNormalJump))
                    {
                        if (CecilUtils.IsJump(instruction.OpCode))
                        {
                            var idx = body.Instructions.IndexOf(instruction.Operand as Instruction);
                            var targetIdx = body.Instructions.IndexOf(jmpTarget);
                            if (instruction.Operand == injectionPoint)
                            {
                                instruction.Operand = initial;
                            }
                        }
                    }
                }
            }
        }

        private Instruction GetReturnByRelativity(MethodDefinition md, int index)
        {
            var instructions = md.Body.Instructions;
            var counter = 0;
            for (var i = 0; i < instructions.Count; i++)
            {
                var ins = instructions[i];
                if (ins.OpCode == OpCodes.Ret)
                {
                    if (counter == index)
                    {
                        return ins;
                    }
                    counter++;
                }
            }
            return null;
        }        
    }

    public class Config
    {
        public string LanguageName { get; set; }
    }

    public struct EventCallbackParams
    {
        public string OwnerType { get; set; }
        public string OwnerMethodName { get; set; }
        public string OwnerMethodDesc { get; set; }
        public string CallbackType { get; set; }
        public string InstanceCallbackName { get; set; }
        public string InstanceCallbackDesc { get; set; }
        public string StaticCallbackName { get; set; }
        public string StaticCallbackDesc { get; set; }
        public bool PushParams { get; set; }
        public bool JumpFix { get; set; }
        public InsertionType InsertionType { get; set; }
        public int[] InsertionIndex { get; set; }
    }

    public enum InsertionType
    {
        BEGINNING,
        ABSOLUTE,
        LAST,
        RETURNS
    }

    public class DetourEvent
    {
        private object returnValue;

        public bool ReturnEarly { get; set; }

        public object ReturnValue
        {
            get { return returnValue; }
            set
            {
                returnValue = value;
                ReturnEarly = true;
            }
        }
    }

    public static class CecilUtils
    {
        public static string DescriptionOf(MethodDefinition md)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            var set = false;
            foreach (var param in md.Parameters)
            {
                sb.Append(param.ParameterType.Resolve().FullName);
                sb.Append(',');
                set = true;
            }
            if (set) sb.Length -= 1;

            sb.Append(')');
            sb.Append(md.ReturnType.Resolve().FullName);
            return sb.ToString();
        }

        public static bool IsGettingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Ldfld || ins.OpCode == OpCodes.Ldflda;
        }

        public static bool IsPuttingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Stfld;
        }

        public static bool IsNativeType(string returnName)
        {
            return returnName.Equals(typeof(long).FullName) ||
                returnName.Equals(typeof(ulong).FullName) ||
                returnName.Equals(typeof(int).FullName) ||
                returnName.Equals(typeof(uint).FullName) ||
                returnName.Equals(typeof(short).FullName) ||
                returnName.Equals(typeof(ushort).FullName) ||
                returnName.Equals(typeof(byte).FullName) ||
                returnName.Equals(typeof(bool).FullName);
        }

        public static bool IsJump(OpCode oc)
        {
            return
                oc == OpCodes.Br || oc == OpCodes.Br_S ||
                oc == OpCodes.Brtrue || oc == OpCodes.Brtrue_S ||
                oc == OpCodes.Brfalse || oc == OpCodes.Brfalse_S ||
                oc == OpCodes.Bne_Un || oc == OpCodes.Bne_Un_S ||
                oc == OpCodes.Blt_Un || oc == OpCodes.Blt_Un_S ||
                oc == OpCodes.Ble_Un || oc == OpCodes.Ble_Un_S ||
                oc == OpCodes.Bge_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Bgt_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Beq || oc == OpCodes.Beq_S ||
                oc == OpCodes.Ble || oc == OpCodes.Ble_S ||
                oc == OpCodes.Blt || oc == OpCodes.Blt_S
                ;
        }

        public static MethodDefinition GetMethod(this AssemblyDefinition asm, string type, string name, string desc)
        {
            var tds = asm.Modules.Where(m => m.GetType(type) != null).Select(m => m.GetType(type));
            if (tds.Count() == 0)
            {
                return null;
            }
            if (tds.Count() != 1)
            {
                throw new Exception();
            }
            var td = tds.First();
            return td.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc.Replace(" ", string.Empty)));
        }
    }
}
