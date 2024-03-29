﻿using MyGame.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyGame
{
    public partial class Form1 : Form
    {
        public Player Player { get; set; }
        public UFO UFO { get; set; }
        public static System.Windows.Forms.Timer Timer { get; set; }
        public Random Random { get; set; }
        static List<Alien> AliensList { get; set; }
        static List<Fuel> FuelsList { get; set; }

        public Form1()
        {
            #region Инициализация иконок
            var healthLabel = new Label();
            healthLabel.Text = "Health:";
            healthLabel.Size = new Size(45, 20);

            var healthBar = new ProgressBar();
            healthBar.Size = new Size(180, 20);
            healthBar.Value = 100;

            var fuelLabel = new Label();
            fuelLabel.Text = "Fuel:";
            fuelLabel.Size = new Size(45, 20);

            var fuelBar = new ProgressBar();
            fuelBar.Size = new Size(180, 20);
            fuelBar.Value = 0;

            var restartButton = new Button();
            restartButton.Text = "Restart game";
            restartButton.AutoSize = true;
            restartButton.Click += (sender, args) => Application.Restart();

            var victoryLabel = new Label();
            victoryLabel.Text = "You WON!!!";
            victoryLabel.Size = new Size(restartButton.Width, restartButton.Height);


            InitializeComponent();
            InitializeEntities();
            Controls.Add(healthLabel);
            Controls.Add(healthBar);
            Controls.Add(fuelLabel);
            Controls.Add(fuelBar);

            SizeChanged += (sender, args) =>
            {
                healthBar.Location = new Point(ClientSize.Width - healthBar.Size.Width, 0);
                healthLabel.Location = new Point(healthBar.Location.X - healthLabel.Size.Width, 0);
                fuelLabel.Location = new Point(0, 0);
                fuelBar.Location = new Point(fuelLabel.Width, 0);
                var resolution = Screen.PrimaryScreen.Bounds.Size;
                victoryLabel.Location = new Point(resolution.Width / 2, resolution.Height / 2);
                restartButton.Location = new Point(victoryLabel.Left, victoryLabel.Bottom);
            };
            #endregion

            DoubleBuffered = true;

            Paint += (sender, args) =>
            {
                if (!Player.InsideUFO)
                {
                    args.Graphics.DrawImage(Player.CurrentSprite.Image, Player.X, Player.Y);
                    foreach (var fuel in FuelsList)
                        args.Graphics.DrawImage(fuel.CurrentSprite.Image, fuel.CurrentSprite.Location);
                }

                foreach (var alien in AliensList)
                    args.Graphics.DrawImage(alien.CurrentSprite.Image, alien.CurrentSprite.Location);
                
                args.Graphics.DrawImage(UFO.CurrentSprite.Image, UFO.CurrentSprite.Location);
                
            };

            MakeAliens(10);
            SpawnFuel();

            Timer.Interval = 30;
            Timer.Tick += (sender, args) =>
            {
                healthBar.Value = Player.Health;
                CheckIfPlayerIsAlive(restartButton);

                CheckPlayerInsideUFO();

                CheckIfTookFuel(fuelBar);
                CheckIfUFOIsFueled(fuelBar, restartButton, victoryLabel);

                CheckAliens();

                Invalidate();
            };
            Timer.Start();

            KeyDown += (sender, args) =>
            {
                if (Player.Health <= 0)
                    return;
                Player.IsMoving = true;
                MoveOrStopPlayer(args.KeyCode, 10, true);
                Shoot(args);
            };

            KeyUp += (sender, args) =>
            {
                MoveOrStopPlayer(args.KeyCode, 0, false);
                CheckIfPlayerIsMoving();
            };            
        }

        private void InitializeEntities()
        {
            Player = new Player();
            UFO = new UFO();
            Timer = new System.Windows.Forms.Timer();
            Random = new Random();
            AliensList = new List<Alien>();
            FuelsList = new List<Fuel>();
        }

        private void MakeAliens(int numberAliens)
        {
            for (int i = 0; i < numberAliens; i++)
            {
                var alien = new Alien(Random);
                AliensList.Add(alien);
            }
        }

        private async void SpawnFuel()
        {
            await SpawnFuelInThread();
        }

        private Task SpawnFuelInThread()
        {
            var task = Task.Run
                (
                    () =>
                    {
                        var randomTime = Random.Next(5000, 13000);
                        Thread.Sleep(randomTime);
                        lock (FuelsList)
                        {
                            FuelsList.Add(new Fuel(this, Random));
                        }
                        SpawnFuel();
                    }
                );
            return task;
        }

        private void CheckIfPlayerIsAlive(Button restartButton)
        {
            if (Player.Health <= 0)
            {
                Player.CurrentSprite.Image = Resource1.DoomGuyDied;
                Controls.Add(restartButton);
                Timer.Stop();
            }
        }

        private void CheckPlayerInsideUFO()
        {
            if (!Player.InsideUFO)
            {
                if (Player.IsMoving)
                    Player.Move(UFO);
                DirectAliensToPlayer();
            }
        }

        private void DirectAliensToPlayer()
        {
            foreach (var alien in AliensList)
                alien.GoToPlayer(Player);
        }

        private async void CheckIfTookFuel(ProgressBar fuelBar)
        {
            var indexesToDelete = await CheckIfTookFuelInThread(fuelBar);
            await RemoveTakenFuelInThread(indexesToDelete);
        }

        private Task<List<int>> CheckIfTookFuelInThread(ProgressBar fuelBar)
        {
            var task = Task.Run
                (
                    () =>
                    {
                        var indexesToDelete = new List<int>();

                        foreach (var fuel in FuelsList)
                        {
                            if (fuel.CurrentSprite.Bounds.IntersectsWith(Player.CurrentSprite.Bounds))
                            {
                                BeginInvoke(new Action(() => 
                                {
                                    if (fuelBar.Value + 15 > 100)
                                        fuelBar.Value = 100;
                                    else
                                        fuelBar.Value += 15;
                                } ));
                                indexesToDelete.Add(FuelsList.IndexOf(fuel));
                            }
                        }
                        return indexesToDelete;
                    }
                );
            return task;
        }

        private Task RemoveTakenFuelInThread(List<int> indexesToDelete)
        {
            var task = Task.Run
                (
                    () =>
                    {
                        foreach (var index in indexesToDelete)
                        {
                            lock (FuelsList)
                            {
                                FuelsList.RemoveAt(index);
                            }
                        }
                    }
                );
            return task;
        }

        private void CheckIfUFOIsFueled(ProgressBar fuelBar, Button restartButton, Label victoryLabel)
        {
            if (fuelBar.Value == 100)
            {
                UFO.CurrentSprite.Image = Resource1.UFOWithFuel;
                UFO.IsFueled = true;
                if (UFO.CurrentSprite.Bounds.Contains(Player.CurrentSprite.Location) || Player.InsideUFO)
                {
                    Player.InsideUFO = true;
                    UFO.CurrentSprite.Image = Resource1.PlayerInsideUFO;
                    UFO.FlyUp();
                    Controls.Add(victoryLabel);
                    Controls.Add(restartButton);
                }
            }
        }

        private async void CheckAliens()
        {
            foreach (var alien in AliensList)
            {
                await CheckIfAliensWereShot(alien);
                CheckIfAliensAttackingPlayer(alien);
            }
        }

        private Task CheckIfAliensWereShot(Alien alien)
        {
            var task = Task.Run
                (
                    () =>
                    {
                        foreach (Control control in this.Controls)
                        {
                            if ((string)control.Tag == "bullet" && alien.CurrentSprite.Bounds.IntersectsWith(control.Bounds))
                            {
                                BeginInvoke(new Action(() =>
                                {
                                    alien.Health--;
                                    this.Controls.Remove(control);
                                    control.Dispose();
                                }));

                                if (alien.Health <= 0)
                                {
                                    BeginInvoke(new Action(() =>
                                    {
                                        alien.CurrentSprite.Location = Alien.GetCoordinate(Random);
                                        alien.Health = 2;
                                        alien.AlienCanGo = false;
                                    }));
                                    var randomTime = Random.Next(3000, 13000);
                                    Thread.Sleep(randomTime);
                                    BeginInvoke(new Action(() => { alien.AlienCanGo = true; }));
                                }
                            }
                        }
                    }
                );
            return task;
        }

        private void CheckIfAliensAttackingPlayer(Alien alien)
        {
            if (!Player.InsideUFO && alien.CurrentSprite.Bounds
                .IntersectsWith(Player.CurrentSprite.Bounds))
            {
                Player.Health -= 2;
            }
        }

        private void MoveOrStopPlayer(Keys keys, int speed, bool isMoving)
        {
            switch (keys)
            {
                case Keys.W:
                    Player.DirectionY = -speed;
                    Player.CurrentMovement[Player.DirectionMovement.Up] = isMoving;
                    Player.CurrentSprite.Image = Resource1.DoomGuyGoingUp;
                    break;
                case Keys.S:
                    Player.DirectionY = speed;
                    Player.CurrentMovement[Player.DirectionMovement.Down] = isMoving;
                    Player.CurrentSprite.Image = Resource1.DoomGuyGoingDown;
                    break;
                case Keys.A:
                    Player.DirectionX = -speed;
                    Player.CurrentMovement[Player.DirectionMovement.Left] = isMoving;
                    Player.CurrentSprite.Image = Resource1.DoomGuyGoingLeft;
                    break;
                case Keys.D:
                    Player.DirectionX = speed;
                    Player.CurrentMovement[Player.DirectionMovement.Right] = isMoving;
                    Player.CurrentSprite.Image = Resource1.DoomGuyGoingRight;
                    break;
            }
        }

        private void Shoot(KeyEventArgs args)
        {
            switch (args.KeyCode)
            {
                case Keys.Right:
                    Player.CurrentSprite.Image = Resource1.DoomGuyShootsRight;
                    Shot(Keys.Right);
                    break;
                case Keys.Left:
                    Player.CurrentSprite.Image = Resource1.DoomGuyShootsLeft;
                    Shot(Keys.Left);
                    break;
                case Keys.Up:
                    Player.CurrentSprite.Image = Resource1.DoomGuyShootsUp;
                    Shot(Keys.Up);
                    break;
                case Keys.Down:
                    Player.CurrentSprite.Image = Resource1.DoomGuyShootsDown;
                    Shot(Keys.Down);
                    break;
            }
        }

        private void Shot(Keys direction)
        {
            var shootBulet = new Bullet();
            shootBulet.Direction = direction;
            shootBulet.CurrentSprite.Location 
                = new Point(Player.X + (Player.CurrentSprite.Width / 2),
                            Player.Y + (Player.CurrentSprite.Height / 2));
            shootBulet.MakeBullet(this);
        }

        private void CheckIfPlayerIsMoving()
        {
            var movement = Player.CurrentMovement
                            .Where(x => x.Value == true)
                            .Select(x => x.Key);
                            
            if (movement.Count() == 0)
            {
                Player.CurrentSprite.Image = Resource1.DoomGuyStand;
                Player.IsMoving = false;
            }
            else if (movement.First() == Player.DirectionMovement.Up)
                MoveOrStopPlayer(Keys.W, 10, true);
            else if (movement.First() == Player.DirectionMovement.Down)
                MoveOrStopPlayer(Keys.S, 10, true);
            else if (movement.First() == Player.DirectionMovement.Right)
                MoveOrStopPlayer(Keys.D, 10, true);
            else if (movement.First() == Player.DirectionMovement.Left)
                MoveOrStopPlayer(Keys.A, 10, true);
        }        
    }
}