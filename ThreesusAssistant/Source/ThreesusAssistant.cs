using System;
using System.Collections.Generic;
using Threesus.Bots;
using Threesus.CoreGame;

namespace Threesus
{
	/// <summary>
	/// An assistant that runs a Threes AI for the purposes of assisting the player play the actual game of Threes.
	/// </summary>
	static class ThreesusAssistant
	{
		private static readonly IBot _bot = new StandardBotFramework(6, 3, BoardQualityEvaluators.OpennessMatthew);

		/// <summary>
		/// Main application entry point.
		/// </summary>
		private static void Main()
		{
			// Build the board and initialize the deck.
			Deck deck = new Deck(new Rand());
			Board board = new Board();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("THREESUS - AI-Assisted Threes Solver\n");
			Console.ResetColor();
			Console.WriteLine("Enter the game board configuration to begin.");
			Console.WriteLine("* Each line is a list of values separated by commas (for example: 0, 1, 2, 3).");
			Console.WriteLine("* Any valid card value may be entered (up to 6144). Invalid values, such as 0, will count as an empty space.\n");
			for (int y = 0; y < board.Height; y++)
			{
				Console.Write("Enter row {0}: ", y + 1);
				string rowStr = Console.ReadLine();
				rowStr = rowStr.Replace(" ", "");
				string[] numbers = rowStr.Split(',');
				/*
				if(rowStr.Length != board.Width)
				{
					Console.WriteLine("Invalid length of entered row.");
					y--;
					continue;
				}
				*/
				if(numbers.Length != board.Width)
                {
					Console.WriteLine("Invalid number of cards entered to row, please try again.");
					y--;
					continue;
                }

				for(int x = 0; x < board.Width; x++)
				{
					Card card = GetCardFromChar(numbers[x], false);
					if(card != null)
					{
						board[x, y] = card;
						deck.RemoveCard(card.Value);
					}
				}
			}
			//Console.WriteLine("\nThanks - board and deck successfully initialized!");
			Console.Clear();

			Stack<Board> boardsStack = new Stack<Board>();
			Stack<Deck> decksStack = new Stack<Deck>();

			// Now let's play!
			while(true)
			{
			redo:

				// Print the current board status.
				//Console.Clear();
				Console.WriteLine("+------ CURRENT BOARD ------+");
				for(int y = 0; y < board.Height; y++)
				{
					Console.Write("|");
					for(int x = 0; x < board.Width; x++)
					{
						Card c = board[x, y];
						if (c != null)
						{
							switch (c.Value)
							{
								case 1:
									Console.ForegroundColor = ConsoleColor.Cyan;
									break;
								case 2:
									Console.ForegroundColor = ConsoleColor.Magenta;
									break;
							}
							//string cardValue = c.Value.ToString().PadLeft(5, ' ').PadRight(6, ' ') + '|';
							string cardValue = c.Value.ToString().PadLeft(5, ' ');
							Console.Write(cardValue);
							Console.ResetColor();
							Console.Write(" |");
						}
						else
							Console.Write("      |");
					}
					Console.WriteLine();
				}
				Console.WriteLine("+---------------------------+\n");
				//string score = board.GetTotalScore().ToString();
				//score = "SCORE: " + score;
				//Console.WriteLine(score.PadLeft(18, '*').PadRight(29, '*'));
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("SCORE: {0}", board.GetTotalScore());
				Console.ResetColor();
				Console.WriteLine();

				// Get the next card.
				Console.Write("What is the next card (1, 2, 3, or +)? ");
				string nextCardStr;
				string[] validCards = { "1", "2", "3", "+" };
				Card nextCard;

				do
				{
					nextCardStr = Console.ReadLine();
					if(nextCardStr == "undo" && boardsStack.Count > 0 && decksStack.Count > 0) 
					{
						board = boardsStack.Pop();
						deck = decksStack.Pop();
						goto redo;
					}
				}

				while (nextCardStr.Length != 1 || (nextCard = GetCardFromChar(nextCardStr, true)) == null || Array.IndexOf(validCards, nextCardStr) == -1);
				//nextCardStr[0]
				NextCardHint nextCardHint = GetNextCardHint(nextCard);

				// Choose a move.
				Console.Write("\nThinking...");
				ShiftDirection? aiDir = _bot.GetNextMove(new FastBoard(board), new FastDeck(deck), nextCardHint);
				if (aiDir != null)
				{
					Console.Clear();
					//Console.WriteLine("/////////////////////////////\n");
					string moveDir = "//  --->  MOVE " + aiDir.Value.ToString().ToUpper() + "  <---  //";
					string moveBar = "".PadRight(moveDir.Length, '/');
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine(moveBar);
					Console.WriteLine(moveDir);
					//Console.WriteLine("//  --->  MOVE {0}", aiDir.Value.ToString().ToUpper().PadRight(27, ' ').PadRight(29, '/'));
					Console.WriteLine(moveBar + "\n");
					Console.ResetColor();
				}
				else
				{
					Console.WriteLine("NO MORE MOVES.");
					break;
				}

				// Confirm the swipe.
				ShiftDirection? actualDir = aiDir.Value;
				/*do
				{
					Console.Write("What direction did you swipe in? (l, r, u, d, or just hit enter for the suggested swipe) ");
					string dirStr = Console.ReadLine();
					actualDir = GetShiftDirection(dirStr, aiDir.Value);
				}
				while(actualDir == null);*/
				List<IntVector2D> newCardCells = new List<IntVector2D>();
				board.Shift(actualDir.Value, newCardCells);

				// Get the new card location.
				int newCardIndex;
				if(newCardCells.Count > 1)
				{
					Console.WriteLine("Here are the locations where a new card might have appeared:\n");
					for(int y = 0; y < board.Height; y++)
					{
						for(int x = 0; x < board.Width; x++)
						{
							int index = newCardCells.IndexOf(new IntVector2D(x, y));
							if (index >= 0)
							{
								Console.ForegroundColor = ConsoleColor.Yellow;
								Console.Write(" " + "abcd"[index] + " ");
								Console.ResetColor();
							}
							else
								Console.Write(" . ");
						}
						Console.WriteLine();
					}
					Console.Write("\nWhere did the new card appear? ");
					do
					{
						string indexStr = Console.ReadLine();
						if(indexStr.Length == 1)
							newCardIndex = indexStr[0] - 'a';
						else
							newCardIndex = -1;
					}
					while(newCardIndex < 0 || newCardIndex >= newCardCells.Count);
				}
				else
				{
					Console.Write("We already know where the new card appeared!\n");
					newCardIndex = 0;
				}

				// Get new card value.
				int newCardValue;
				if(nextCardHint == NextCardHint.Bonus)
				{
					do
					{
						Console.Write("\nWhat is the value of the new card? ");
					}
					while(!TryGetNewCardValue(Console.ReadLine(), out newCardValue));
				}
				else
				{
					newCardValue = (int)nextCardHint + 1;
				}
				deck.RemoveCard(newCardValue);
				board[newCardCells[newCardIndex]] = new Card(newCardValue, -1);

				boardsStack.Push(new Board(board));
				decksStack.Push(new Deck(deck));
                Console.WriteLine();
			}

			Console.WriteLine("FINAL SCORE IS {0}.", board.GetTotalScore());
		}

		/// <summary>
		/// Gets the card that is indicated by the specified character.
		/// </summary>
		private static Card GetCardFromChar(string c, bool allowBonusCard)
		{
			switch(c)
			{
				case "1":
					return new Card(1, -1);
				case "2":
					return new Card(2, -2);
				case "3":
					return new Card(3, -1);
				case "6":
					return new Card(6, -1);
				case "12":
					return new Card(12, -1);
				case "24":
					return new Card(24, -1);
				case "48":
					return new Card(48, -1);
				case "96":
					return new Card(96, -1);
				case "192":
					return new Card(192, -1);
				case "384":
					return new Card(384, -1);
				case "768":
					return new Card(768, -1);
				case "1536":
					return new Card(1536, -1);
				case "3072":
					return new Card(3072, -1);
				case "6144":
					return new Card(6144, -1);
				case "+":
					if(allowBonusCard)
						return new Card(-1, -1);
					else
						return null;
				default:
					return null;
			}
		}

		/// <summary>
		/// Returns the NextCardHint given the specified next card.
		/// </summary>
		private static NextCardHint GetNextCardHint(Card nextCard)
		{
			NextCardHint nextCardHint;
			switch(nextCard.Value)
			{
				case 1:
					nextCardHint = NextCardHint.One;
					break;
				case 2:
					nextCardHint = NextCardHint.Two;
					break;
				case 3:
					nextCardHint = NextCardHint.Three;
					break;
				default:
					nextCardHint = NextCardHint.Bonus;
					break;
			}
			return nextCardHint;
		}

		/// <summary>
		/// Returns the shift direction as specified by the specified string, or null if none was specified.
		/// If the string has no length, then the defaultDir will be returned.
		/// </summary>
		private static ShiftDirection? GetShiftDirection(string str, ShiftDirection defaultDir)
		{
			if(str.Length == 0)
				return defaultDir;
			else if(str.Length > 1)
				return null;
			else
			{
				switch(str[0])
				{
					case 'l':
						return ShiftDirection.Left;
					case 'r':
						return ShiftDirection.Right;
					case 'u':
						return ShiftDirection.Up;
					case 'd':
						return ShiftDirection.Down;
					default:
						return null;
				}
			}
		}

		/// <summary>
		/// Attempts to extract the value of a new card from the specified string.
		/// </summary>
		private static bool TryGetNewCardValue(string str, out int newCardValue)
		{
			if(int.TryParse(str, out newCardValue))
			{
				// Verify that it's a real card.
				return
					newCardValue == 6 ||
					newCardValue == 12 ||
					newCardValue == 24 ||
					newCardValue == 48 ||
					newCardValue == 96 ||
					newCardValue == 192 ||
					newCardValue == 384 ||
					newCardValue == 768 ||
					newCardValue == 1536 ||
					newCardValue == 3072 ||
					newCardValue == 6144;
			}
			else
			{
				return false;
			}
		}
	}
}