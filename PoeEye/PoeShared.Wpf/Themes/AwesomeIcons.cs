using System.Linq;
using System.Text;
using PoeShared.Scaffolding;

namespace PoeShared.Themes;

public static class AwesomeIcons
{
    public const string Adjust = "\uf042";
    public const string AngleDoubleDown = "\uf103";
    public const string AngleDoubleLeft = "\uf100";
    public const string AngleDoubleRight = "\uf101";
    public const string AngleDoubleUp = "\uf102";
    public const string AngleDown = "\uf107";
    public const string AngleLeft = "\uf104";
    public const string AngleRight = "\uf105";
    public const string AngleUp = "\uf106";
    public const string Archive = "\uf187";
    public const string ArrowCircleUp = "\uf0aa";
    public const string ArrowCircleOutlinedUp = "\uf01b";
    public const string ArrowCircleDown = "\uf0ab";
    public const string ArrowCircleOutlinedDown = "\uf01a";
    public const string AuraHasOverlay = "\uf03e";
    public const string OnEnterActions = "\uf090";
    public const string WhileActiveActions = "\uf021";
    public const string OnExitActions = "\uf08b";
    public const string EnablingCondition = PowerOff;
    public const string CaretRight = "\uf0da";
    public const string Certificate = "\uf0a3";
    public const string Check = "\uf00c";
    public const string CheckCircle = "\uf058";
    public const string ChevronLeft = "\uf053";
    public const string ChevronRight = "\uf054";
    public const string ChevronUp = "\uf077";
    public const string ChevronDown = "\uf078";
    public const string CloseIcon = "\uf00d";
    public const string CloudDownload = "\uf0ed";
    public const string CloudUpload = "\uf0ee";
    public const string CogIcon = "\uf013";
    public const string Copy = "\uF0C5";
    public const string CollapsedDirectory = "\uf114";
    public const string CheckboxSquareChecked = "\uf046";
    public const string CheckboxSquareEmpty = "\uf096";
    public const string CheckboxSquareMinus = "\uf147";
    public const string Crosshair = "\uf05b";
    public const string Download = "\uf019";
    public const string Eyedropper = "\uf1fb";
    public const string EditIcon = "\uf044";
    public const string ErrorIcon = "\uf06a";
    public const string ExpandedDirectory = "\uf115";
    public const string Exclamation = "\uf06a";
    public const string File = "\uf016";
    public const string FileText = "\uf0f6";
    public const string FileImage = "\uf1c5";
    public const string Font = "\uf031";
    public const string FolderOpen = "\uf115";
    public const string Keyboard = "\uf11c";
    public const string Hourglass = "\uf252";
    public const string Incognito = "\uf2a7";
    public const string InfoIcon = "\uf05a";
    public const string IsActive = "\uf05d";
    public const string IsLoadedIcon = "\uf10c";
    public const string IsNotActive = "\uf10c";
    public const string IsDisabled = QuestionCircle;
    public const string IsNotLoaded = "\uf070";
    public const string Trigger = "\uf0e7";
    public const string LinkIcon = "\uf0c1";
    public const string ListUlIcon = "\uf0ca";
    public const string MousePointer = "\uf245";
    public const string Padlock = "\uf023";
    public const string PlayOutlined = "\uf01d";
    public const string PlusCircle = "\uf055";
    public const string Paste = "\uF0EA";
    public const string Pause = "\uf04c";
    public const string PowerOff = "\uf011";
    public const string Repeat = "\uf01e";
    public const string Reply = "\uf112";
    public const string ReplyAll = "\uf122";
    public const string QuestionCircle = "\uf29c";
    public const string Random = "\uf074";
    public const string Reorder = "\uf0c9";
    public const string Refresh = "\uf021";
    public const string RotateLeft = Undo;
    public const string RotateRight = Repeat;
    public const string Save = "\uf0c7";
    public const string Search = "\uf002";
    public const string SearchMinus = "\uf010";
    public const string SearchPlus = "\uf00e";
    public const string SignIn = "\uf090";
    public const string SignOut = "\uf08b";
    public const string SortAmountAsc = "\uf160";
    public const string SortAmountDesc = "\uf161";
    public const string SortNumericAsc = "\uf162";
    public const string SortNumericDesc = "\uf163";
    public const string Space = " ";
    public const string StopOutlined = "\uf28e";
    public const string Stop = "\uf28d";
    public const string Tachometer = "\uf0e4";
    public const string Thermometer = "\uf2c7";
    public const string Tree = "\uf1bb";
    public const string Undo = "\uf0e2";
    public const string Unlink = "\uf127";
    public const string Warning = "\uf071";
    public const string Wiki = "\uf266";
    public const string WindowMaximize = "\uf2d0";
    public const string WindowRestore = "\uf2d2";
    public const string Youtube = "\uf167";
    public const string User = "\uf007";
    public const string UserCircle = "\uf2bd";
    public const string UserSecret = "\uf21b";
    public const string Zoom = "\uf002";

    public static string ToHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Select(x => $"&#{(ushort) x};").JoinStrings(string.Empty);
    }
}