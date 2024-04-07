using Godot;

namespace Mnes.Ui.Godot.Nodes.Ui;

// This spaghetti code is a WIP
// "It just works"
public partial class MainUI : Control {
   const string TEXTURE_RECT_ID = "%MNES TextureRect";
   const string FOLDER_SELECT_DIALOG_ID = "%Folder Select Dialog";
   TextureRect logo;
   FileDialog folder_select;

	public override void _Ready() {
      logo = GetNode<TextureRect>(TEXTURE_RECT_ID);
      folder_select = GetNode<FileDialog>(FOLDER_SELECT_DIALOG_ID);

      FadeInLogo();
   }

   void FadeInLogo() {
      logo.Modulate = new Color();

      using var d = DirAccess.Open("");

      // Global position doesn't update immediately, just create a fast tween that does it
      var move_out = CreateTween();
      move_out.SetEase(Tween.EaseType.Out);
      move_out.SetTrans(Tween.TransitionType.Back);
      move_out.TweenProperty(logo, "global_position", new Vector2(0, -100f), 0.01f);

      var fade_in = CreateTween();
      fade_in.SetEase(Tween.EaseType.Out);
      fade_in.SetTrans(Tween.TransitionType.Back);
      fade_in.TweenProperty(logo, "modulate", new Color(1f, 1f, 1f, 1f), 3f).SetDelay(0.3f);
      fade_in.SetParallel(true);

      var move_in = CreateTween();
      move_in.SetEase(Tween.EaseType.Out);
      move_in.SetTrans(Tween.TransitionType.Back);
      move_in.TweenProperty(logo, "global_position", new Vector2(0, 0), 1.5f).SetDelay(0.3f);
   }

   public void BtnFolderSelectDown() => 
      folder_select.Popup();

   public void FldlDirectorySelected(string folder) =>
      MainScene.Instance.AddGamesDirectory(folder);
}
