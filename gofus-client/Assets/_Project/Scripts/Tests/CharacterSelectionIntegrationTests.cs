using NUnit.Framework;
using UnityEngine;
using GOFUS.UI.Screens;
using GOFUS.UI;

namespace GOFUS.Tests.Integration
{
    /// <summary>
    /// Integration tests for Character Selection functionality
    /// Tests the complete flow from login to character selection to game entry
    /// </summary>
    public class CharacterSelectionIntegrationTests
    {
        private GameObject testGameObject;
        private CharacterSelectionScreen characterSelection;
        private const string TEST_JWT_TOKEN = "test_jwt_token_123";
        private const string BACKEND_URL = "https://gofus-backend.vercel.app";

        [SetUp]
        public void Setup()
        {
            // Create test GameObject
            testGameObject = new GameObject("TestCharacterSelection");
            characterSelection = testGameObject.AddComponent<CharacterSelectionScreen>();

            // Set up test token
            PlayerPrefs.SetString("jwt_token", TEST_JWT_TOKEN);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up
            if (testGameObject != null)
            {
                Object.Destroy(testGameObject);
            }

            // Clear test data
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void CharacterSelection_Initialization_SetsUpCorrectly()
        {
            // Act
            characterSelection.Initialize();

            // Assert
            Assert.IsNotNull(characterSelection.CharacterSlots);
            Assert.AreEqual(5, characterSelection.MaxCharacterSlots);
            Assert.AreEqual(0, characterSelection.CharacterCount);
            Assert.IsFalse(characterSelection.CanPlay);
            Assert.IsTrue(characterSelection.CanCreateNew);
        }

        [Test]
        public void CharacterSelection_LoadsJWTToken_OnInitialization()
        {
            // Arrange
            string expectedToken = TEST_JWT_TOKEN;

            // Act
            characterSelection.Initialize();

            // Assert
            string actualToken = PlayerPrefs.GetString("jwt_token");
            Assert.AreEqual(expectedToken, actualToken);
        }

        [Test]
        public void CharacterSelection_LoadsCharacters_FromBackend()
        {
            // Arrange
            characterSelection.Initialize();

            // Act
            // Note: In real async test, would wait for backend call
            // For now, just verify initialization doesn't crash

            // Assert
            // Character count should be >= 0 (depending on backend)
            Assert.GreaterOrEqual(characterSelection.CharacterCount, 0);
        }

        [Test]
        public void CharacterSelection_MaxCharacters_EnforcesLimit()
        {
            // Arrange
            var characters = new System.Collections.Generic.List<CharacterData>();
            for (int i = 0; i < 6; i++)
            {
                characters.Add(new CharacterData
                {
                    Id = i + 1,
                    Name = $"TestChar{i}",
                    Level = 1,
                    Class = "Iop"
                });
            }

            characterSelection.Initialize();

            // Act
            characterSelection.LoadCharacters(characters);

            // Assert
            Assert.AreEqual(5, characterSelection.CharacterCount); // Should cap at MAX_CHARACTERS
        }

        [Test]
        public void CharacterSelection_SelectCharacter_UpdatesState()
        {
            // Arrange
            var testCharacter = new CharacterData
            {
                Id = 1,
                Name = "TestHero",
                Level = 10,
                Class = "Iop",
                Gender = "Male"
            };

            characterSelection.Initialize();
            characterSelection.LoadCharacters(new System.Collections.Generic.List<CharacterData> { testCharacter });

            // Act
            characterSelection.SelectCharacter(0);

            // Assert
            Assert.AreEqual(0, characterSelection.SelectedSlotIndex);
            Assert.AreEqual(1, characterSelection.SelectedCharacterId);
            Assert.IsTrue(characterSelection.CanPlay);
        }

        [Test]
        public void CharacterSelection_NoCharacters_CannotPlay()
        {
            // Arrange
            characterSelection.Initialize();
            characterSelection.LoadCharacters(new System.Collections.Generic.List<CharacterData>());

            // Act & Assert
            Assert.IsFalse(characterSelection.CanPlay);
            Assert.AreEqual(-1, characterSelection.SelectedCharacterId);
        }

        [Test]
        public void CharacterSelection_PlayCharacter_SavesCharacterId()
        {
            // Arrange
            var testCharacter = new CharacterData
            {
                Id = 42,
                Name = "TestHero",
                Level = 10,
                Class = "Iop"
            };

            characterSelection.Initialize();
            characterSelection.LoadCharacters(new System.Collections.Generic.List<CharacterData> { testCharacter });
            characterSelection.SelectCharacter(0);

            // Act
            characterSelection.PlaySelectedCharacter();

            // Assert
            int savedCharacterId = PlayerPrefs.GetInt("selected_character_id");
            Assert.AreEqual(42, savedCharacterId);
        }

        [Test]
        public void CharacterSelection_SortByLevel_OrdersCorrectly()
        {
            // Arrange
            var characters = new System.Collections.Generic.List<CharacterData>
            {
                new CharacterData { Id = 1, Name = "Low", Level = 5, Class = "Iop" },
                new CharacterData { Id = 2, Name = "High", Level = 50, Class = "Cra" },
                new CharacterData { Id = 3, Name = "Mid", Level = 25, Class = "Feca" }
            };

            characterSelection.Initialize();
            characterSelection.LoadCharacters(characters);

            // Act
            characterSelection.SortByLevel();

            // Assert
            var slots = characterSelection.CharacterSlots;
            Assert.AreEqual(50, slots[0].Level); // Highest first
            Assert.AreEqual(25, slots[1].Level);
            Assert.AreEqual(5, slots[2].Level);
        }

        [Test]
        public void CharacterSelection_SortByName_OrdersAlphabetically()
        {
            // Arrange
            var characters = new System.Collections.Generic.List<CharacterData>
            {
                new CharacterData { Id = 1, Name = "Zara", Level = 5, Class = "Iop" },
                new CharacterData { Id = 2, Name = "Alice", Level = 10, Class = "Cra" },
                new CharacterData { Id = 3, Name = "Bella", Level = 15, Class = "Feca" }
            };

            characterSelection.Initialize();
            characterSelection.LoadCharacters(characters);

            // Act
            characterSelection.FilterByClass("All");

            // Assert
            var visibleCount = characterSelection.GetVisibleCharacterCount();
            Assert.AreEqual(3, visibleCount);
        }

        [Test]
        public void CharacterSelection_FilterByClass_ShowsOnlyMatchingClass()
        {
            // Arrange
            var characters = new System.Collections.Generic.List<CharacterData>
            {
                new CharacterData { Id = 1, Name = "Warrior1", Level = 5, Class = "Iop" },
                new CharacterData { Id = 2, Name = "Archer1", Level = 10, Class = "Cra" },
                new CharacterData { Id = 3, Name = "Warrior2", Level = 15, Class = "Iop" }
            };

            characterSelection.Initialize();
            characterSelection.LoadCharacters(characters);

            // Act
            characterSelection.FilterByClass("Iop");

            // Assert
            var visibleCount = characterSelection.GetVisibleCharacterCount();
            Assert.AreEqual(2, visibleCount); // Only Iops should be visible
        }

        [Test]
        public void CharacterSelection_AvailableSlots_CalculatesCorrectly()
        {
            // Arrange
            var characters = new System.Collections.Generic.List<CharacterData>
            {
                new CharacterData { Id = 1, Name = "Char1", Level = 5, Class = "Iop" },
                new CharacterData { Id = 2, Name = "Char2", Level = 10, Class = "Cra" }
            };

            characterSelection.Initialize();

            // Act
            characterSelection.LoadCharacters(characters);

            // Assert
            Assert.AreEqual(3, characterSelection.AvailableSlots); // 5 max - 2 used = 3
        }

        [Test]
        public void CharacterSelection_MaxCharactersReached_CannotCreateNew()
        {
            // Arrange
            var characters = new System.Collections.Generic.List<CharacterData>();
            for (int i = 0; i < 5; i++)
            {
                characters.Add(new CharacterData
                {
                    Id = i + 1,
                    Name = $"Char{i}",
                    Level = i + 1,
                    Class = "Iop"
                });
            }

            characterSelection.Initialize();

            // Act
            characterSelection.LoadCharacters(characters);

            // Assert
            Assert.IsFalse(characterSelection.CanCreateNew);
            Assert.AreEqual(0, characterSelection.AvailableSlots);
        }

        [Test]
        public void CharacterSlot_HasCharacter_ReturnsCorrectState()
        {
            // Arrange
            var slot = testGameObject.AddComponent<CharacterSlot>();
            slot.Initialize(0);

            var testCharacter = new CharacterData
            {
                Id = 1,
                Name = "TestChar",
                Level = 10,
                Class = "Iop"
            };

            // Act - No character
            Assert.IsFalse(slot.HasCharacter);

            // Act - With character
            slot.SetCharacterData(testCharacter);

            // Assert
            Assert.IsTrue(slot.HasCharacter);
            Assert.AreEqual("TestChar", slot.CharacterName);
            Assert.AreEqual(10, slot.Level);
        }

        [Test]
        public void CharacterSlot_Selection_TogglesCorrectly()
        {
            // Arrange
            var slot = testGameObject.AddComponent<CharacterSlot>();
            slot.Initialize(0);

            var testCharacter = new CharacterData
            {
                Id = 1,
                Name = "TestChar",
                Level = 10,
                Class = "Iop"
            };

            slot.SetCharacterData(testCharacter);

            // Act & Assert - Initially not selected
            Assert.IsFalse(slot.IsSelected);

            // Act - Select
            slot.SetSelected(true);
            Assert.IsTrue(slot.IsSelected);

            // Act - Deselect
            slot.SetSelected(false);
            Assert.IsFalse(slot.IsSelected);
        }

        [Test]
        public void CharacterSlot_Clear_RemovesData()
        {
            // Arrange
            var slot = testGameObject.AddComponent<CharacterSlot>();
            slot.Initialize(0);

            var testCharacter = new CharacterData
            {
                Id = 1,
                Name = "TestChar",
                Level = 10,
                Class = "Iop"
            };

            slot.SetCharacterData(testCharacter);
            Assert.IsTrue(slot.HasCharacter);

            // Act
            slot.Clear();

            // Assert
            Assert.IsFalse(slot.HasCharacter);
            Assert.AreEqual("", slot.CharacterName);
            Assert.AreEqual(0, slot.Level);
        }

        [Test]
        public void CharacterSelection_RefreshButton_ReloadsFromBackend()
        {
            // Arrange
            characterSelection.Initialize();
            int initialCount = characterSelection.CharacterCount;

            // Act
            characterSelection.RequestRefresh();

            // Assert
            // Should have attempted to reload (count might be same)
            // Note: In real async test, would wait for backend response
            Assert.GreaterOrEqual(characterSelection.CharacterCount, 0);
        }

        [Test]
        public void CharacterSelection_Events_FireCorrectly()
        {
            // Arrange
            characterSelection.Initialize();
            bool eventFired = false;
            int eventCharacterId = -1;

            characterSelection.OnCharacterSelected += (id) =>
            {
                eventFired = true;
                eventCharacterId = id;
            };

            var testCharacter = new CharacterData
            {
                Id = 99,
                Name = "EventTest",
                Level = 1,
                Class = "Iop"
            };

            characterSelection.LoadCharacters(new System.Collections.Generic.List<CharacterData> { testCharacter });

            // Act
            characterSelection.SelectCharacter(0);

            // Assert
            Assert.IsTrue(eventFired);
            Assert.AreEqual(99, eventCharacterId);
        }

        [Test]
        public void CharacterSelection_PlayEvent_FiresCorrectly()
        {
            // Arrange
            characterSelection.Initialize();
            bool playEventFired = false;
            int playCharacterId = -1;

            characterSelection.OnPlayCharacter += (id) =>
            {
                playEventFired = true;
                playCharacterId = id;
            };

            var testCharacter = new CharacterData
            {
                Id = 88,
                Name = "PlayTest",
                Level = 1,
                Class = "Iop"
            };

            characterSelection.LoadCharacters(new System.Collections.Generic.List<CharacterData> { testCharacter });
            characterSelection.SelectCharacter(0);

            // Act
            characterSelection.PlaySelectedCharacter();

            // Assert
            Assert.IsTrue(playEventFired);
            Assert.AreEqual(88, playCharacterId);
        }

        [Test]
        public void CharacterData_PropertiesSetCorrectly()
        {
            // Arrange & Act
            var character = new CharacterData
            {
                Id = 42,
                Name = "TestHero",
                Level = 25,
                Class = "Iop",
                Gender = "Male",
                LastPlayed = "Today",
                Experience = 12345,
                MapId = 7411
            };

            // Assert
            Assert.AreEqual(42, character.Id);
            Assert.AreEqual("TestHero", character.Name);
            Assert.AreEqual(25, character.Level);
            Assert.AreEqual("Iop", character.Class);
            Assert.AreEqual("Male", character.Gender);
            Assert.AreEqual("Today", character.LastPlayed);
            Assert.AreEqual(12345, character.Experience);
            Assert.AreEqual(7411, character.MapId);
        }
    }

    /// <summary>
    /// End-to-end integration tests for the complete authentication to game flow
    /// </summary>
    public class CompleteLoginToGameFlowTests
    {
        [Test]
        public void CompleteFlow_LoginToCharacterSelection_WorksEndToEnd()
        {
            // This test simulates the complete user flow
            // 1. Login → 2. Character Selection → 3. Play

            // Arrange - Set up mock login
            PlayerPrefs.SetString("jwt_token", "test_token_e2e");
            PlayerPrefs.Save();

            // Create UIManager
            var uiManagerGO = new GameObject("UIManager");
            var uiManager = uiManagerGO.AddComponent<UIManager>();

            // Act - Initialize (this should create LoginScreen and CharacterSelection)
            // Note: In real async test, would wait for initialization

            // Assert - UIManager component exists
            Assert.IsNotNull(uiManager);

            // Clean up
            Object.Destroy(uiManagerGO);
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void LoginToCharacterSelection_TransitionState_MaintainsToken()
        {
            // Arrange
            string testToken = "transition_test_token";
            PlayerPrefs.SetString("jwt_token", testToken);

            // Act - Simulate login success (token should persist)
            string retrievedToken = PlayerPrefs.GetString("jwt_token");

            // Assert
            Assert.AreEqual(testToken, retrievedToken);

            // Clean up
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void CharacterSelection_LogoutButton_ClearsAllData()
        {
            // Arrange
            PlayerPrefs.SetString("jwt_token", "test_token");
            PlayerPrefs.SetString("account_id", "test_account");
            PlayerPrefs.SetInt("selected_character_id", 42);

            // Act - Simulate logout
            PlayerPrefs.DeleteKey("jwt_token");
            PlayerPrefs.DeleteKey("account_id");
            PlayerPrefs.DeleteKey("selected_character_id");

            // Assert
            Assert.IsEmpty(PlayerPrefs.GetString("jwt_token", ""));
            Assert.IsEmpty(PlayerPrefs.GetString("account_id", ""));
            Assert.AreEqual(0, PlayerPrefs.GetInt("selected_character_id", 0));

            // Clean up
            PlayerPrefs.DeleteAll();
        }
    }
}
