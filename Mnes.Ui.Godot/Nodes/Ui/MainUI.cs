using Godot;
using Mnes.Core;
using Mnes.Core.Machine.Input;
using Mnes.Core.Saves.Configuration;

namespace Mnes.Ui.Godot.Nodes.Ui;

// This spaghetti code is a WIP
// "It just works"
public partial class MainUI : Control {
   const string TEXTURE_RECT_ID = "%MNES TextureRect";
   const string FOLDER_SELECT_DIALOG_ID = "%Folder Select Dialog";
   const string ROM_SELECT_DIALOG_ID = "%Rom Select Dialog";
   const string EMULATION_ID = "%Emulation";

   TextureRect? _logo;
   FileDialog? _folder_select;
   FileDialog? _rom_select;
   Emulation? _emulation;

   TextureRect Logo => _logo ?? throw new InvalidOperationException($"{nameof(_logo)} was null; this probably means that {nameof(_Ready)} was not called");
   FileDialog FolderSelect => _folder_select ?? throw new InvalidOperationException($"{nameof(_folder_select)} was null; this probably means that {nameof(_Ready)} was not called");
   FileDialog RomSelect => _rom_select ?? throw new InvalidOperationException($"{nameof(_rom_select)} was null; this probably means that {nameof(_Ready)} was not called");
   Emulation Emulation => _emulation ?? throw new InvalidOperationException($"{nameof(_emulation)} was null; this probably means that {nameof(_Ready)} was not called");

   public override void _Ready() {
      _logo = GetNode<TextureRect>(TEXTURE_RECT_ID);
      _folder_select = GetNode<FileDialog>(FOLDER_SELECT_DIALOG_ID);
      _rom_select = GetNode<FileDialog>(ROM_SELECT_DIALOG_ID);
      _emulation = GetNode<Emulation>(EMULATION_ID);

      FadeInLogo();
   }

   void FadeInLogo() {
      Logo.Modulate = new Color();

      using var d = DirAccess.Open("");

      // This should go somewhere else but I'm not sure where yet
      // Global position doesn't update immediately, just create a fast tween that does it
      var move_out = CreateTween();
      move_out.SetEase(Tween.EaseType.Out);
      move_out.SetTrans(Tween.TransitionType.Back);
      move_out.TweenProperty(_logo, "global_position", new Vector2(0, -100f), 0.01f);

      var fade_in = CreateTween();
      fade_in.SetEase(Tween.EaseType.Out);
      fade_in.SetTrans(Tween.TransitionType.Back);
      fade_in.TweenProperty(_logo, "modulate", new Color(1f, 1f, 1f, 1f), 3f).SetDelay(0.3f);
      fade_in.SetParallel();

      var move_in = CreateTween();
      move_in.SetEase(Tween.EaseType.Out);
      move_in.SetTrans(Tween.TransitionType.Back);
      move_in.TweenProperty(_logo, "global_position", new Vector2(0, 0), 1.5f).SetDelay(0.3f);
   }

   public void BtnOpenRom() =>
      RomSelect.Popup();

   public void BtnFolderSelectDown() =>
      FolderSelect.Popup();

   public void FldlDirectorySelected(string folder) =>
      MainScene.Instance.AddGamesDirectory(folder);

   public void FldlRomSelected(string rom) {
      NesInputState input = new();
      Emulation.Emulator = new MnesEmulator(rom, new MnesConfig(), input);
      Emulation.Emulator.Run();
      DisplayServer.WindowSetTitle("MNES - Playing " + Path.GetFileName(rom));
   }
}
