using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Checkers
{
    public class CheckersBoard : MonoBehaviour
    {
        [Tooltip("Prefabs for Checker Pieces")]
        public GameObject whitePiecePrefab, blackPiecePrefab;
        [Tooltip("Where to attach the spawned pieces in the Hierarchy")]
        public Transform checkersParent;
        public Vector3 boardOffset = new Vector3(-4.0f, 0.0f, -4.0f);
        public Vector3 pieceOffset = new Vector3(.5f, 0, .5f);
        public float rayDistance = 1000f;
        public LayerMask hitLayers;

        public Piece[,] pieces = new Piece[8, 8];

        /*
         * isHost = Is the player currently the host? (for networking)
         * isWhiteTurn = Is it current player's turn or opponent?
         * hasKilled = Did the player's piece get killed?
         */
        private bool isWhiteTurn = true, hasKilled;
        private Vector2 mouseOver, startDrag, endDrag;

        private Piece selectedPiece = null;

        private void Start()
        {
            GenerateBoard();
        }
        private void Update()
        {
            // Update the mouse over information
            MouseOver();
            // Is it currently white's turn?
            if (isWhiteTurn)
            {
                // Get x and y coordinate of selected mouse over
                int x = (int)mouseOver.x;
                int y = (int)mouseOver.y;
                // If the mouse is pressed
                if (Input.GetMouseButtonDown(0))
                {
                    // Try selecting piece
                    selectedPiece = SelectPiece(x, y);
                    startDrag = new Vector2(x, y);
                }
                // If there is a selected piece
                if (selectedPiece)
                {
                    // Move the piece with Mouse
                    DragPiece(selectedPiece);
                }
                // If button is released
                if (Input.GetMouseButtonUp(0))
                {
                    endDrag = new Vector2(x, y); // Record end drag
                    TryMove(startDrag, endDrag); // Try moving the piece
                    selectedPiece = null; // Let go of the piece
                }
            }
        }

        /// <summary>
        /// Generates a Checker Piece in specified coordinates
        /// </summary>
        /// <param name="x">X Location</param>
        /// <param name="y">Y Location</param>
        public void GeneratePiece(int x, int y, bool isWhite)
        {
            // What prefab are we using (white or black) ?
            GameObject prefab = isWhite ? whitePiecePrefab : blackPiecePrefab;
            // Generate Instance of prefab
            GameObject clone = Instantiate(prefab, checkersParent);
            // Get the Piece component
            Piece p = clone.GetComponent<Piece>();
            // Update Piece X & Y with Current Location
            p.x = x;
            p.y = y;
            // Reposition clone
            MovePiece(p, x, y);
        }

        /// <summary>
        /// Clears and re-generates entire board 
        /// </summary>
        public void GenerateBoard()
        {
            // Generate White Team
            for (int y = 0; y < 3; y++)
            {
                bool oddRow = y % 2 == 0;
                // Loop through columns
                for (int x = 0; x < 8; x += 2)
                {
                    // Generate Piece
                    GeneratePiece(oddRow ? x : x + 1, y, true);
                }
            }
            // Generate Black Team
            for (int y = 5; y < 8; y++)
            {
                bool oddRow = y % 2 == 0;
                // Loop through columns
                for (int x = 0; x < 8; x += 2)
                {
                    // Generate Piece
                    GeneratePiece(oddRow ? x : x + 1, y, false);
                }
            }
        }

        /// <summary>
        /// Selects a piece on the 2D grid and returns it
        /// </summary>
        /// <param name="x">X Coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns></returns>
        private Piece SelectPiece(int x, int y)
        {
            // Check if X and Y is out of bounds
            if (OutOfBounds(x, y))
                // Return result early
                return null;

            // Get the piece at X and Y location
            Piece piece = pieces[x, y];

            // Check that it is't null
            if (piece)
                return piece;

            return null;
        }

        /// <summary>
        /// Moves a Piece to another coordinate on a 2D grid
        /// </summary>
        /// <param name="p">The Piece to move</param>
        /// <param name="x">X Location</param>
        /// <param name="y">Y Location</param>
        private void MovePiece(Piece p, int x, int y)
        {
            // Update array
            pieces[p.x, p.y] = null;
            pieces[x, y] = p;
            p.x = x;
            p.y = y;
            // Translate the piece to another location
            p.transform.localPosition = new Vector3(x, 0, y) + boardOffset + pieceOffset;
        }

        /// <summary>
        /// Updating when the pieces have been selected
        /// </summary>
        private void MouseOver()
        {
            // Perform Raycast from mouse position
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // If the ray hit the board
            if (Physics.Raycast(camRay, out hit, rayDistance, hitLayers))
            {
                // Convert mouse coordinates to 2D array coordinates
                mouseOver.x = (int)(hit.point.x - boardOffset.x);
                mouseOver.y = (int)(hit.point.z - boardOffset.z);
            }
            else // Otherwise
            {
                // Default to error (-1)
                mouseOver.x = -1;
                mouseOver.y = -1;
            }
        }

        /// <summary>
        /// Drags the selected piece using Raycast location
        /// </summary>
        /// <param name="p"></param>
        private void DragPiece(Piece selected)
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // Detects mouse ray hit point
            if (Physics.Raycast(camRay, out hit, rayDistance, hitLayers))
            {
                // Updates position of selected piece to hit point + offset
                selected.transform.position = hit.point + Vector3.up;
            }
        }

        /// <summary>
        /// Tries moving a piece from Current (x1 + y1) to Desired (x2 + y2) coordinates
        /// </summary>
        /// <param name="x1">Current X</param>
        /// <param name="y1">Current Y</param>
        /// <param name="x2">Desired X</param>
        /// <param name="y2">Desired Y</param>
        private void TryMove(Vector2 start, Vector2 end)
        {
            int x1 = (int)start.x;
            int y1 = (int)start.y;
            int x2 = (int)end.x;
            int y2 = (int)end.y;

            // Note(Manny): Probably won't need this later
            // Record start Drag & end Drag
            startDrag = new Vector2(x1, y1);
            endDrag = new Vector2(x2, y2);

            // If there is a selected piece
            if (selectedPiece)
            {
                // Check if desired location is Out of Bounds
                if (OutOfBounds(x2, y2))
                {
                    // Move it back to original (start)
                    MovePiece(selectedPiece, x1, y1);
                    return; // Exit function!
                }

                // Check if it is a Valid Move
                if (ValidMove(start, end))
                {
                    //  Replace end coordinates with our selected piece
                    MovePiece(selectedPiece, x2, y2);
                }
                else
                {
                    // Move it back to original (start)
                    MovePiece(selectedPiece, x1, y1);
                }
            }
        }

        /// <summary>
        /// Checks if given coordinates are out of the board range
        /// </summary>
        /// <param name="x">X Location</param>
        /// <param name="y">Y Location</param>
        /// <returns></returns>
        private bool OutOfBounds(int x, int y)
        {
            return x < 0 || x >= 8 || y < 0 || y >= 8;
        }

        private bool ValidMove(Vector2 start, Vector2 end)
        {
            int x1 = (int)start.x;
            int y1 = (int)start.y;
            int x2 = (int)end.x;
            int y2 = (int)end.y;

            #region Rule #1 - Is the start the same as the end?
            if (start == end)
            {
                // You can move back where you were
                return true;
            }
            #endregion

            #region Rule #2 - If you are moving on top of another piece
            if (pieces[x2, y2])
            {
                // YA CAN'T DO DAT!
                return false;
            }
            #endregion

            #region Rule #3 - Detect if we're moving diagonal & forwards / backwards

            // Store X change value (abs)
            int locationX = Mathf.Abs(x1 - x2);
            int locationY = y2 - y1;

            // Rule #3.1 - White piece rule
            if (selectedPiece.isWhite || selectedPiece.isKing)
            {
                // Check if we're moving diagonally right
                if (locationX == 1 && locationY == 1)
                {
                    // This is a valid move!
                    return true;
                }
                // If moving diagonally left (two spaces)
                else if (locationX == 2 && locationY == 2)
                {
                    // Get the piece in between move
                    Piece pieceBetween = GetPieceBetween(start, end);
                    // If there is a piece between AND the piece isn't the same color
                    if (pieceBetween != null &&
                       pieceBetween.isWhite != selectedPiece.isWhite)
                    {
                        // You're allowed to move there!
                        return true;
                    }
                }
            }

            // Rule #3.2 - Black piece rule
            if (!selectedPiece.isWhite || selectedPiece.isKing)
            {
                // Check if we're moving diagonally right
                if (locationX == 1 && locationY == -1)
                {
                    // This is a valid move!
                    return true;
                }
                // If moving diagonally left (two spaces)
                else if (locationX == 2 && locationY == -2)
                {
                    // Get the piece in between move
                    Piece pieceBetween = GetPieceBetween(start, end);
                    // If there is a piece between AND the piece isn't the same color
                    if (pieceBetween != null &&
                       pieceBetween.isWhite != selectedPiece.isWhite)
                    {
                        // You're allowed to move there!
                        return true;
                    }
                }
            }

            //print("X Location: " + XLocation + " Y Location:" + YLocation);
            // Add rules here
            // Add rules here
            #endregion
            
            return false;
        }

        /// <summary>
        /// Calculates & returns the piece between start and end locations
        /// </summary>
        /// <param name="start">Start Location</param>
        /// <param name="end">End Location</param>
        /// <returns></returns>
        private Piece GetPieceBetween(Vector2 start, Vector2 end)
        {
            int xIndex = (int)(start.x + end.x) / 2;
            int yIndex = (int)(start.y + end.y) / 2;
            return pieces[xIndex, yIndex];
        }
        /// <summary>
        /// Removes a piece from the board
        /// </summary>
        /// <param name="pieceToRemove"></param>
        private void RemovePiece(Piece pieceToRemove)
        {
            // Remove it from the array
            pieces[pieceToRemove.x, pieceToRemove.y] = null;
            // Destroy the gameobject of the piece immediately
            DestroyImmediate(pieceToRemove.gameObject);
        }
        /// <summary>
        /// Runs after the turn has finished
        /// <summary>
        private void EndTurn()
        {
            CheckForKing();
        }

        /// <summary>
        /// Check if a piece needs to be kinged
        /// </summary>
        void CheckForKing()
        { 
            // Get the end drag locations
            int x = (int)endDrag.x;
            int y = (int)endDrag.y;
            // Checkif the selected piece needs to be kinged
            if (selectedPiece)
            {
                // If the selected piece is white and reached the end of the board
                if(selectedPiece.isWhite &&
                    selectedPiece.isKing == false &&
                    y == 7)
                {
                    // The selected piece is kinged!
                    selectedPiece.isKing = true;
                    // Run animations
                }
                else if(!selectedPiece.isWhite && y == 0)
                {
                    // The selected piece is kinged!
                    selectedPiece.isKing = true;
                    // Run Animations
                }
            }
        }
    }
}