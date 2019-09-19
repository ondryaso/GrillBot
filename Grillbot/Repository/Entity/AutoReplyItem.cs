﻿using Grillbot.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grillbot.Repository.Entity
{
    [Table("AutoReply")]
    public class AutoReplyItem
    {
        [Key]
        [Column]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Column]
        [Required]
        public string MustContains { get; set; }

        [Column]
        [Required]
        public string ReplyMessage { get; set; }

        [Column]
        [Required]
        public bool IsDisabled { get; set; }

        [NotMapped]
        public int CallsCount { get; set; }

        public bool CanReply() => !string.IsNullOrEmpty(ReplyMessage) && !IsDisabled;

        public override string ToString()
        {
            var activeText = IsDisabled ? "Neaktivní" : "Aktivní";

            var mustContains = MustContains.FormatDiscordUrl();
            var reply = ReplyMessage.FormatDiscordUrl();

            return $"**{ID}** - {mustContains} => {reply} - {activeText}";
        }
    }
}