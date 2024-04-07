using Godot;
using System;

namespace Mnes.Ui.Godot.Nodes.Ui;

public partial class MainUI : Control {
   const string TEXTURE_RECT_ID = "%MNES TextureRect";
   TextureRect tr;

	public override void _Ready() {
      tr = GetNode<TextureRect>(TEXTURE_RECT_ID);
      tr.GlobalPosition = new Vector2 (-100, -100);
      tr.Modulate = new Color();
   }

   void FadeInLogo() {
      // Global position doesn't update immediately, just create a fast tween that does it
      var move_out = CreateTween();
      move_out.SetEase(Tween.EaseType.Out);
      move_out.SetTrans(Tween.TransitionType.Back);
      move_out.TweenProperty(tr, "global_position", new Vector2(0, -100f), 0.01f);

      var fade_in = CreateTween();
      fade_in.SetEase(Tween.EaseType.Out);
      fade_in.SetTrans(Tween.TransitionType.Back);
      fade_in.TweenProperty(tr, "modulate", new Color(1f, 1f, 1f, 1f), 3f).SetDelay(3f);
      fade_in.SetParallel(true);

      var move_in = CreateTween();
      move_in.SetEase(Tween.EaseType.Out);
      move_in.SetTrans(Tween.TransitionType.Back);
      move_in.TweenProperty(tr, "global_position", new Vector2(0, 0), 1.5f);
   }

	public override void _Process(double delta) {
	}
}
