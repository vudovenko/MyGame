﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace MyGame
{
    class Bullet
    {
        public Keys Direction { get; set; }
        private int Speed { get; set; }
        public PictureBox CurrentSprite { get; set; }

        public Bullet()
        {
            Speed = 30;
            CurrentSprite = new PictureBox();
            CurrentSprite.BackColor = Color.White;
            CurrentSprite.Size = new Size(4, 4);
            CurrentSprite.Tag = "bullet";
        }

        public void MakeBullet(Form form)
        {
            form.Controls.Add(CurrentSprite);

            Form1.Timer.Tick += (sender, args) =>
            {
                var resolution = Screen.PrimaryScreen.Bounds.Size;
                if (Direction == Keys.Left)
                    CurrentSprite.Left -= Speed;
                if (Direction == Keys.Right)
                    CurrentSprite.Left += Speed;
                if (Direction == Keys.Up)
                    CurrentSprite.Top -= Speed;
                if (Direction == Keys.Down)
                    CurrentSprite.Top += Speed;

                if (CurrentSprite.Left < 0 || CurrentSprite.Left > resolution.Width || CurrentSprite.Top < 10 || CurrentSprite.Top > resolution.Width)
                    CurrentSprite.Dispose();
            };
        }
    }
}
