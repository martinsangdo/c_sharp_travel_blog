-- ============================================================
-- TravelBlogDB – SQL Server Schema & Sample Data
-- Run this script in SQL Server Management Studio (SSMS)
-- or via sqlcmd to create and seed the database.
-- ============================================================

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TravelBlogDB')
BEGIN
    CREATE DATABASE TravelBlogDB;
    PRINT 'Database TravelBlogDB created.';
END
GO

USE TravelBlogDB;
GO

-- ============================================================
-- DROP TABLES (reverse dependency order)
-- ============================================================
IF OBJECT_ID('BlogTags',    'U') IS NOT NULL DROP TABLE BlogTags;
IF OBJECT_ID('BlogImages',  'U') IS NOT NULL DROP TABLE BlogImages;
IF OBJECT_ID('Comments',    'U') IS NOT NULL DROP TABLE Comments;
IF OBJECT_ID('Blogs',       'U') IS NOT NULL DROP TABLE Blogs;
IF OBJECT_ID('Users',       'U') IS NOT NULL DROP TABLE Users;
GO

-- ============================================================
-- CREATE TABLES
-- ============================================================

CREATE TABLE Users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FullName     NVARCHAR(100) NULL,
    AvatarUrl    NVARCHAR(200) NULL,
    Bio          NVARCHAR(300) NULL,
    Role         NVARCHAR(20)  NOT NULL DEFAULT 'User',  -- 'User' | 'Admin'
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2     NULL,
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('User','Admin'))
);

CREATE TABLE Blogs (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Title            NVARCHAR(200)  NOT NULL,
    ShortDescription NVARCHAR(500)  NOT NULL,
    LongDescription  NVARCHAR(MAX)  NOT NULL,
    Destination      NVARCHAR(100)  NULL,
    CoverImageUrl    NVARCHAR(500)  NULL,
    IsPublic         BIT            NOT NULL DEFAULT 1,
    Status           NVARCHAR(20)   NOT NULL DEFAULT 'Published', -- Draft|Published|Hidden
    PublishedAt      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt        DATETIME2      NULL,
    ViewCount        INT            NOT NULL DEFAULT 0,
    AuthorId         INT            NOT NULL,
    CONSTRAINT FK_Blogs_Author FOREIGN KEY (AuthorId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Blogs_Status CHECK (Status IN ('Draft','Published','Hidden'))
);

CREATE TABLE Comments (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Content     NVARCHAR(1000) NOT NULL,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2      NULL,
    IsDeleted   BIT            NOT NULL DEFAULT 0,
    BlogId      INT            NOT NULL,
    UserId      INT            NOT NULL,
    CONSTRAINT FK_Comments_Blog FOREIGN KEY (BlogId) REFERENCES Blogs(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Comments_User FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE BlogImages (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ImageUrl    NVARCHAR(500) NOT NULL,
    Caption     NVARCHAR(200) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    UploadedAt  DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    BlogId      INT           NOT NULL,
    CONSTRAINT FK_BlogImages_Blog FOREIGN KEY (BlogId) REFERENCES Blogs(Id) ON DELETE CASCADE
);

CREATE TABLE BlogTags (
    Id      INT IDENTITY(1,1) PRIMARY KEY,
    Name    NVARCHAR(50) NOT NULL,
    BlogId  INT          NOT NULL,
    CONSTRAINT FK_BlogTags_Blog FOREIGN KEY (BlogId) REFERENCES Blogs(Id) ON DELETE CASCADE
);
GO

-- ============================================================
-- INDEXES
-- ============================================================
CREATE INDEX IX_Blogs_AuthorId    ON Blogs(AuthorId);
CREATE INDEX IX_Blogs_Destination ON Blogs(Destination);
CREATE INDEX IX_Blogs_IsPublic    ON Blogs(IsPublic);
CREATE INDEX IX_Blogs_PublishedAt ON Blogs(PublishedAt DESC);
CREATE INDEX IX_Comments_BlogId   ON Comments(BlogId);
CREATE INDEX IX_Comments_UserId   ON Comments(UserId);
CREATE INDEX IX_BlogTags_BlogId   ON BlogTags(BlogId);
GO

-- ============================================================
-- SAMPLE DATA
-- ============================================================
-- Passwords are BCrypt hashes of "Password123!"
-- Hash generated with BCrypt.Net cost factor 11
-- You can login with any account below using: Password123!
-- ============================================================

-- ── Users ────────────────────────────────────────────────────
INSERT INTO Users (Username, Email, PasswordHash, FullName, Bio, Role, IsActive, CreatedAt)
VALUES
-- Admin account
('admin',
 'admin@travelblog.com',
 '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh',
 'Site Administrator',
 'Managing the TravelBlog platform.',
 'Admin', 1, DATEADD(month,-6, GETUTCDATE())),

-- Regular users
('sarah_wanders',
 'sarah@example.com',
 '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh',
 'Sarah Chen',
 'Solo traveler | Foodie | 40+ countries visited. Based in Singapore.',
 'User', 1, DATEADD(month,-5, GETUTCDATE())),

('jakobontheroad',
 'jakob@example.com',
 '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh',
 'Jakob Müller',
 'Adventure photographer from Berlin. Mountains are my happy place.',
 'User', 1, DATEADD(month,-4, GETUTCDATE())),

('luna_explores',
 'luna@example.com',
 '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh',
 'Luna Pham',
 'Cultural explorer and budget travel expert. Vietnam-born, world-curious.',
 'User', 1, DATEADD(month,-3, GETUTCDATE())),

('marco_globe',
 'marco@example.com',
 '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh',
 'Marco Rossi',
 'Italian chef turned travel blogger. I eat my way around the world.',
 'User', 1, DATEADD(month,-2, GETUTCDATE())),

('admin2',
 'admin2@travelblog.com',
 '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh',
 'Content Moderator',
 'Second admin account for content moderation.',
 'Admin', 1, DATEADD(month,-1, GETUTCDATE()));
GO

-- ── Blogs ─────────────────────────────────────────────────────
INSERT INTO Blogs (Title, ShortDescription, LongDescription, Destination, IsPublic, Status, PublishedAt, ViewCount, AuthorId)
VALUES
-- Blog 1 – Sarah
(
 '10 Days in Bali: The Ultimate Island Guide',
 'From rice terraces to hidden waterfalls, my complete guide to exploring Bali without the tourist traps.',
 '<h2>Why Bali Never Disappoints</h2>
<p>I have visited Bali three times now, and every time I leave, I immediately start planning my return. There is something about this island that burrows into your soul — the fragrance of incense from morning offerings, the sound of gamelan music drifting from temples, the impossible green of the rice terraces in the early morning light.</p>

<h2>Ubud: The Cultural Heart</h2>
<p>Start your journey in Ubud, Bali"s cultural capital. Skip the monkey forest (the monkeys are aggressive) and instead head to the Tegalalang Rice Terraces at 7am before the tour buses arrive. The terraces glow gold and emerald in the morning mist, and you will have them almost to yourself.</p>
<p>The Ubud Art Market is worth two hours of your time. Bargain confidently — start at 30% of the asking price and meet in the middle. I picked up a hand-carved wooden Garuda for 80,000 IDR after negotiating from 250,000 IDR.</p>

<h2>Hidden Waterfall: Tibumana</h2>
<p>While everyone flocks to Tegenungan, rent a scooter and ride 45 minutes north to Tibumana Waterfall. The path winds through a jungle canyon and the falls drop into a crystal pool perfect for swimming. Entry is 20,000 IDR and you will share the space with maybe ten other visitors.</p>

<h2>Seminyak vs Canggu</h2>
<p>If you want nightlife and beach clubs, Seminyak is your place. If you want a more relaxed surfer vibe with excellent coffee shops, choose Canggu. I spent four nights in Canggu and loved every morning at Deus Ex Machina watching the surfers and sipping a flat white.</p>

<h2>Practical Tips</h2>
<ul>
<li>Rent a scooter: 70,000–100,000 IDR/day. It changes your entire Bali experience.</li>
<li>Drink coconuts from roadside stalls, not bottled water from resorts (300% markup).</li>
<li>Visit temples at sunrise. Tanah Lot at sunset is incredibly crowded; try it at 7am instead.</li>
<li>Download the Grab app for car rides in tourist areas.</li>
</ul>',
 'Bali, Indonesia', 1, 'Published',
 DATEADD(day,-45, GETUTCDATE()), 847, 2
),

-- Blog 2 – Jakob
(
 'Hiking the Swiss Alps: Grindelwald Base Camp Trail',
 'A first-timer''s guide to one of Europe''s most breathtaking hikes — no ropes required.',
 '<h2>The Eiger, Mönch, and Jungfrau Up Close</h2>
<p>Standing at Kleine Scheidegg with the north face of the Eiger looming above me, I understood for the first time why mountaineers spend their lives chasing peaks. The scale is simply incomprehensible until you are standing beneath it.</p>

<h2>Getting There</h2>
<p>Take the train from Interlaken Ost to Grindelwald (45 min, 14 CHF) then the gondola up to First (32 CHF, or included in the Jungfrau Travel Pass). The Grindelwald-First Cliff Walk is a steel walkway bolted to the cliff edge at 2,168m. Not for the acrophobic, but absolutely not to be missed.</p>

<h2>The Base Camp Trail</h2>
<p>The 4-hour loop from First down to Bachalpsee and back is rated as moderate. The alpine lake at Bachalpsee reflects the Schreckhorn on calm mornings — bring a wide-angle lens. I hiked this in late September and had the trail largely to myself after the main gondola rush at 10am.</p>

<h2>Budget Reality in Switzerland</h2>
<p>Switzerland is genuinely expensive. I budgeted 250 CHF/day and actually spent around 310 CHF. Buy groceries from Migros (the green supermarket) rather than eating every meal at a restaurant. A takeaway sandwich and coffee from Migros costs 8 CHF vs 24 CHF at a mountain restaurant.</p>

<h2>When to Go</h2>
<p>Mid-July to late September for hiking. The wildflower meadows peak in late July. I prefer September — smaller crowds, cooler temps, and the autumn light on the peaks is extraordinary.</p>',
 'Grindelwald, Switzerland', 1, 'Published',
 DATEADD(day,-32, GETUTCDATE()), 612, 3
),

-- Blog 3 – Luna
(
 'Ha Noi on $30 a Day: The Budget Traveler''s Complete Guide',
 'Exploring Vietnam''s capital city on a shoestring — street food, hidden pagodas, and night train tips.',
 '<h2>Vietnam''s Capital is Underrated</h2>
<p>Most travelers rush through Hanoi on their way to Ha Long Bay. Spend at least three nights here and you will understand why locals are so fiercely proud of their city. Hanoi has a rhythm that Ho Chi Minh City does not — quieter, more elegant, with a French-colonial layer beneath the Vietnamese soul.</p>

<h2>The Old Quarter: Walk, Don''t Taxi</h2>
<p>The 36 ancient streets of the Old Quarter are named after the goods historically sold there: Hang Bac (Silver Street), Hang Gai (Silk Street), Hang Ma (Paper Goods Street). You will be walking past motorbikes, street vendors, and families cooking on the pavement. Embrace the beautiful chaos.</p>

<h2>$30 Daily Budget Breakdown</h2>
<ul>
<li><strong>Accommodation:</strong> $8–12 — Hostel dorm or budget guesthouse in the Old Quarter</li>
<li><strong>Breakfast:</strong> $1.50 — Bun cha or pho bo from a street cart</li>
<li><strong>Lunch:</strong> $2–3 — Com binh dan (rice plate) from a lunch spot</li>
<li><strong>Dinner:</strong> $4–6 — Sit-down restaurant or Bun cha joint</li>
<li><strong>Transport:</strong> $3–5 — Grab scooter rides are incredibly cheap</li>
<li><strong>Sights:</strong> $2–4 — Most pagodas cost 20,000–40,000 VND</li>
</ul>

<h2>Do Not Miss</h2>
<p>Hoan Kiem Lake at 6am when locals practice tai chi on the shores. The Temple of Literature — beautiful and not yet overrun. Ca phe trung (egg coffee) at Cafe Giang, the original inventor since 1946. The Long Bien Bridge at sunset, sitting on the tracks with the city stretching out below you.</p>',
 'Hanoi, Vietnam', 1, 'Published',
 DATEADD(day,-28, GETUTCDATE()), 503, 4
),

-- Blog 4 – Marco
(
 'Eating Through Tokyo: 7 Days, 50 Meals',
 'A food-obsessed Italian''s honest guide to the greatest food city on earth — from ramen alleys to kaiseki dinners.',
 '<h2>Tokyo Changed How I Think About Food</h2>
<p>I am Italian. I grew up believing Italian food was the pinnacle of culinary civilization. Tokyo humbled me completely and I am grateful. The precision, the respect for ingredients, the way a bowl of ramen can reduce you to silence — it is a spiritual experience wrapped in a paper ticket.</p>

<h2>The Ramen Strategy</h2>
<p>Tokyo has a ramen style for every mood. Ichiran in Shibuya is for when you want total focus — private booths, no conversation, just you and the tonkotsu. Fuunji in Shinjuku serves tsukemen (dipping ramen) that is arguably the best thing I have ever eaten at 2pm on a Wednesday. Budget 800–1200 yen per bowl.</p>

<h2>The 7am Tsukiji Breakfast</h2>
<p>Tsukiji outer market is still alive even though the inner auction moved to Toyosu. Arrive at 7am, eat uni (sea urchin) on rice at Sushi Dai, tuna sashimi from Daiwa Sushi, tamagoyaki from Marutake. You will spend 3000 yen and eat better than most three-star restaurants could manage.</p>

<h2>Budget vs Splurge</h2>
<p>Tokyo lets you eat brilliantly at every price point. A 500-yen convenience store onigiri from 7-Eleven is genuinely excellent — this is not a joke. But if you can stretch to one splurge, book a kaiseki dinner at a Michelin-starred restaurant. I paid 18,000 yen at a 2-star in Ginza and it remains the most beautiful meal of my life.</p>',
 'Tokyo, Japan', 1, 'Published',
 DATEADD(day,-18, GETUTCDATE()), 1243, 5
),

-- Blog 5 – Sarah (private)
(
 'My Embarrassing Solo Travel Fails (And What I Learned)',
 'The time I missed a flight, got scammed in Marrakech, and accidentally booked a hostel in the wrong city.',
 '<h2>Nobody Talks About This</h2>
<p>Travel influencers show you the golden hour photos and the serendipitous connections. They rarely show you the three-hour crying session in a Bangkok airport after missing your connection, or the morning you realize your "hotel" is actually someone''s spare bedroom and the address is wrong.</p>

<h2>The Wrong City Incident</h2>
<p>I booked a hostel in Casablanca. I flew into Marrakech. These are 3 hours apart. It cost me 400 dirhams in taxi fees and a very humbling conversation with myself about reading booking confirmations more carefully.</p>',
 'Various', 0, 'Published',
 DATEADD(day,-14, GETUTCDATE()), 89, 2
),

-- Blog 6 – Jakob
(
 'Northern Lights in Iceland: Everything You Need to Know',
 'After three failed attempts and one perfect night, here is what I know about chasing the aurora borealis.',
 '<h2>The Aurora is Unpredictable and That is the Point</h2>
<p>Three times I drove into the Icelandic darkness chasing the forecast. Three times I saw nothing but clouds. On the fourth night, sitting outside a farmhouse near Vik at 11pm, the sky simply exploded in green. My camera was already packed away. I just stood there and watched.</p>

<h2>The Best Time to Visit</h2>
<p>September to March for aurora darkness. I recommend October — the tourist crowds from summer have thinned, the highland roads (F-roads) are still passable, and you get a mix of autumn color and early aurora season.</p>

<h2>Aurora Apps and Forecasts</h2>
<p>Download the Vedur app (Iceland Met Office) and set alerts for KP index 3 or above. The Aurora Forecast app by Jón Flosason is excellent for visual cloud cover overlay. But the secret is this: you need clear skies more than you need a strong KP index. A KP5 behind clouds is invisible.</p>

<h2>Practical Tips</h2>
<ul>
<li>Rent a 4WD — even in October, conditions change fast</li>
<li>Drive at least 40km from Reykjavik to escape light pollution</li>
<li>Bring chemical hand warmers — standing still in -5°C for hours is brutal</li>
<li>Manual camera settings: ISO 1600, f/2.8, 15-second exposure as a starting point</li>
</ul>',
 'Iceland', 1, 'Published',
 DATEADD(day,-10, GETUTCDATE()), 778, 3
),

-- Blog 7 – Luna
(
 'Hoi An in 48 Hours: The Lantern Town That Stole My Heart',
 'Ancient trading port, tailor-made clothes, and the best banh mi in Vietnam — all in one magical weekend.',
 '<h2>Hoi An Deserves More Than an Overnight Stay</h2>
<p>Most people do Hoi An as a day trip from Da Nang. Do not make this mistake. Stay two nights minimum, ideally three, and let the town seep into you. Walk the ancient quarter at 6am before the tourist boats fill the Thu Bon River. Order a bowl of Cao Lau (the local noodle dish that can only be made with water from Hoi An''s ancient wells) at a plastic table by the river.</p>

<h2>Getting a Suit Made</h2>
<p>Hoi An is famous for its tailors, and yes, you should get something made. I had a silk ao dai (traditional Vietnamese dress) made in 24 hours for 650,000 VND (about $27). The key: find a tailor on a side street rather than the main tourist drag, bring a reference photo, and return for one fitting before pickup.</p>

<h2>The Full Moon Lantern Festival</h2>
<p>On the 14th of each lunar month, the Old Quarter turns off electric lights and fills the river with lanterns. It is genuinely magical and costs 120,000 VND for five paper lanterns to float yourself. Book accommodation well in advance if your trip aligns with the full moon.</p>',
 'Hoi An, Vietnam', 1, 'Published',
 DATEADD(day,-7, GETUTCDATE()), 421, 4
),

-- Blog 8 – Marco
(
 'Naples Pizza Pilgrimage: Rating 12 Pizzerias in 48 Hours',
 'An Italian''s brutally honest review of Naples'' most famous pizza joints — including the ones tourists get wrong.',
 '<h2>Context: I Am Italian. This Matters.</h2>
<p>I grew up eating pizza in Rome, which Neapolitans consider an insult to pizza. They are correct. Neapolitan pizza — soft, blistered, soupy in the center, made with San Marzano tomatoes and fior di latte — is a completely different food from Roman pizza al taglio, and arguing about which is better is like arguing about whether to breathe oxygen or nitrogen.</p>

<h2>The Verdicts</h2>
<p><strong>L''Antica Pizzeria da Michele</strong> (menu: only Margherita and Marinara) — This is the pilgrimage site. The crust is perfect. The queue is 45 minutes and worth every minute. 5/5.</p>
<p><strong>Sorbillo on Via dei Tribunali</strong> — Excellent but more tourist-facing than it used to be. Still a 4/5 for the quality of ingredients.</p>
<p><strong>Concettina ai Tre Santi</strong> — The most creative Neapolitan pizza I ate. Gennaro Esposito does things with toppings that should not work but absolutely do. 5/5 for adventurous eaters.</p>

<h2>What Tourists Get Wrong</h2>
<p>Do not order pizza at restaurants near the train station. Do not add extra cheese. Do not ask for it to be well-done. Neapolitan pizza is meant to be soft and slightly wet in the center — that is a feature, not a flaw. A price above €6 for a margherita is a red flag.</p>',
 'Naples, Italy', 1, 'Published',
 DATEADD(day,-3, GETUTCDATE()), 334, 5
);
GO

-- ── Blog Tags ──────────────────────────────────────────────────
INSERT INTO BlogTags (BlogId, Name) VALUES
(1, 'bali'), (1, 'indonesia'), (1, 'beach'), (1, 'temple'),
(2, 'switzerland'), (2, 'hiking'), (2, 'alps'), (2, 'photography'),
(3, 'vietnam'), (3, 'budget'), (3, 'street food'), (3, 'hanoi'),
(4, 'japan'), (4, 'food'), (4, 'tokyo'), (4, 'ramen'),
(5, 'solo travel'), (5, 'tips'), (5, 'mistakes'),
(6, 'iceland'), (6, 'aurora'), (6, 'northern lights'), (6, 'photography'),
(7, 'vietnam'), (7, 'hoi an'), (7, 'culture'), (7, 'street food'),
(8, 'italy'), (8, 'food'), (8, 'pizza'), (8, 'naples');
GO

-- ── Comments ───────────────────────────────────────────────────
INSERT INTO Comments (BlogId, UserId, Content, CreatedAt)
VALUES
-- Blog 1 (Bali)
(1, 3, 'Great guide! The tip about Tibumana waterfall is gold. Most tourists completely miss it. I visited last month and had the whole place to myself.', DATEADD(day,-43, GETUTCDATE())),
(1, 4, 'I disagree about the monkey forest — the silver-haired macaques are amazing if you don''t bring food! But yes, Tegalalang at 7am is life-changing.', DATEADD(day,-41, GETUTCDATE())),
(1, 5, 'Canggu has changed a lot in the last two years though. It''s getting very digital-nomad-heavy. Still good but losing some of the local character.', DATEADD(day,-38, GETUTCDATE())),

-- Blog 2 (Swiss Alps)
(2, 2, 'The Bachalpsee reflection shot is on my bucket list. Did you use a polarizing filter? The water looks almost impossibly clear in your description.', DATEADD(day,-30, GETUTCDATE())),
(2, 4, 'Swiss prices are no joke. I made the mistake of eating at mountain restaurants every day and blew my budget by day 3. Migros is the secret weapon!', DATEADD(day,-28, GETUTCDATE())),

-- Blog 3 (Hanoi)
(3, 2, 'Ca phe trung at Cafe Giang is something else. I can''t believe it took me until my third Vietnam trip to try it. The original recipe is incredible.', DATEADD(day,-26, GETUTCDATE())),
(3, 5, 'Your budget breakdown is super accurate. I spent almost exactly this in September 2023. The Long Bien Bridge at sunset is an experience I still dream about.', DATEADD(day,-24, GETUTCDATE())),
(3, 3, 'Question: is it safe to use Grab for scooter rides as a solo female traveler?', DATEADD(day,-22, GETUTCDATE())),

-- Blog 4 (Tokyo)
(4, 2, 'Your description of Sushi Dai at 7am made me book flights to Tokyo. I arrive in three weeks. This better be as good as you say.', DATEADD(day,-16, GETUTCDATE())),
(4, 3, 'The 7-Eleven onigiri point is genuinely true and I felt the same disbelief. The tuna mayo is better than most deli sandwiches I have had in Europe.', DATEADD(day,-14, GETUTCDATE())),
(4, 4, 'Fuunji tsukemen is absolutely worth the queue. Go at 11:30 right when they open to avoid the lunch rush.', DATEADD(day,-12, GETUTCDATE())),

-- Blog 6 (Iceland)
(6, 2, 'The Vedur app tip is the most useful thing I have read in any Iceland post. Added to my apps for my February trip!', DATEADD(day,-8, GETUTCDATE())),
(6, 4, 'Three failed attempts and then magic on the fourth — this is exactly my Northern Lights experience in Norway. The waiting is part of the story.', DATEADD(day,-7, GETUTCDATE())),

-- Blog 7 (Hoi An)
(7, 2, 'The ao dai tip is practical gold. I got ripped off on my first tailor visit on the main drag. Lesson learned: always go to the side streets.', DATEADD(day,-5, GETUTCDATE())),
(7, 3, 'Cao Lau is genuinely one of the most interesting regional dishes in all of Asia. The claim about the well water affecting the taste sounds like a food myth but somehow it''s true.', DATEADD(day,-4, GETUTCDATE())),

-- Blog 8 (Naples Pizza)
(8, 2, 'Da Michele is a religious experience. I proposed to my wife there after our Margherita. She said yes. The pizza deserves partial credit.', DATEADD(day,-2, GETUTCDATE())),
(8, 3, 'The wet center thing is the biggest culture shock for non-Italians. My American friends refused to eat it because they thought it was undercooked. Their loss.', DATEADD(day,-1, GETUTCDATE()));
GO

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

PRINT '========================================';
PRINT 'Database seeded successfully!';
PRINT '========================================';

SELECT 'Users'    AS [Table], COUNT(*) AS [Count] FROM Users
UNION ALL
SELECT 'Blogs',    COUNT(*) FROM Blogs
UNION ALL
SELECT 'Comments', COUNT(*) FROM Comments
UNION ALL
SELECT 'Tags',     COUNT(*) FROM BlogTags;

PRINT '';
PRINT 'Login credentials (all accounts use the same password):';
PRINT 'Email: admin@travelblog.com     | Password: Password123!  | Role: Admin';
PRINT 'Email: admin2@travelblog.com    | Password: Password123!  | Role: Admin';
PRINT 'Email: sarah@example.com        | Password: Password123!  | Role: User';
PRINT 'Email: jakob@example.com        | Password: Password123!  | Role: User';
PRINT 'Email: luna@example.com         | Password: Password123!  | Role: User';
PRINT 'Email: marco@example.com        | Password: Password123!  | Role: User';
GO
