<div dir="rtl" lang="he">

# אור במנהרה: שלב הלימוד

מטלה 3 בקורס פיתוח משחקי מחשב. מימוש ראשוני של שלב הלימוד במשחק "אור במנהרה".

השחקן מתעורר בתא במנהרה. הוא צריך לצאת למסדרון, לקחת בקבוק מים ולחזור לתא, בלי שהמצלמה תראה אותו.

## קישורים

* המשחק באתר המשחקים: [game-dev-ariel.itch.io/light-in-the-tunnel](https://game-dev-ariel.itch.io/light-in-the-tunnel)
* סרטונים מהטלפון (אנדרואיד):
  * [לאורך](Videos/phone-portrait.mp4)
  * [לרוחב](Videos/phone-landscape.mp4)

## מה מימשנו (חלק ב)

| דרישה | איך היא ממומשת | קבצים |
|---|---|---|
| דגמים ויצירה אוטומטית, כולל דגמים מבינה מלאכותית | בקבוק המים נוצר מדגם בכל פעם שאין בקבוק בשלב. התמונות של הבקבוק והמצלמה נוצרו בבינה מלאכותית | `Items/BottleSpawner.cs`, `Items/WaterBottle.cs` |
| התנגשויות | הקירות עוצרים את השחקן. איסוף הבקבוק, המסירה במזרן, הזיכרונות והרמזים עובדים דרך התנגשויות | `Player/PlayerController.cs`, `Items/MattressGoal.cs`, `Level/MemoryZone.cs`, `Level/HintZone.cs` |
| ניהול שלבים ושמירת ערכים | מנהל משחק יחיד (סינגלטון עם `DontDestroyOnLoad`) שומר את התקווה, את מספר הפעמים שראו אותך ואת הזמן. מסך הסיום מציג אותם | `Core/GameManager.cs`, `UI/EndScreen.cs` |
| התאמה לטלפון | ג'ויסטיק על המסך במכשיר עם מסך מגע. הממשק והמצלמה מתאימים את עצמם לגודל המסך ולמצב לאורך או לרוחב | `UI/MobileJoystick.cs`, `Camera/OrientationCameraSize.cs` |

המצלמה רואה דרך קרניים שנעצרות בקירות (`Security/SecurityCamera.cs`), והמפה נבנית מתרשים טקסט (`Editor/TutorialMapBuilder.cs`). כל הקבצים נמצאים תחת `Assets/Scripts`.

**משחקים:** במחשב, `WASD` או החצים. בטלפון, הג'ויסטיק בפינה.

## תכנון השלבים (חלק א)

לכל אחד מהמשחקים שלנו תכננו ארבעה שלבים: הדרכה, קל, בינוני וקשה.

* אור במנהרה: [story-and-levels.md](https://github.com/Game-development-Ariel/Light-in-the-Tunnel/blob/main/story-and-levels.md)
* ביטקוין קבור: [levels.md](https://github.com/Game-development-Ariel/Buried-Bitcoin/blob/main/levels.md)
* בר הבלנדים: [levels.md](https://github.com/Game-development-Ariel/Vino-Blend-Bar/blob/main/levels.md)

## חברי הצוות

* דוד בלוך ([davidbloch18](https://github.com/davidbloch18))
* עומרי סבג ([omrisabag2001](https://github.com/omrisabag2001))

</div>
